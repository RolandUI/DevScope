using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using RolandUI.DevScope.AnimationTiming;

namespace RolandUI.DevScope.Tests.AnimationTiming;

internal sealed class AvaloniaAnimationClockAdapterTests
{
    private static readonly AnimationClockCompatibility SupportedCompatibility =
        new(true, "Test clock API is supported.");

    [Test]
    public void Avalonia12_1InternalClockShapeIsSupported()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var compatibility = AvaloniaAnimationClockAdapter.Instance.Compatibility;

            Assert.Multiple(() =>
            {
                Assert.That(compatibility.IsSupported, Is.True);
                Assert.That(compatibility.Diagnostic, Does.Contain("experimental").IgnoreCase);
                Assert.That(compatibility.Diagnostic, Does.Contain("version-sensitive").IgnoreCase);
            });
        });
    }

    [Test]
    public void PauseResumeStepResetAndReleaseAreDeterministic()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var source = new ManualAnimationTimeSource();
            var adapter = new AvaloniaAnimationClockAdapter(SupportedCompatibility, () => source);
            var animatedChild = new Border { Opacity = 0d };
            var target = new StackPanel { Children = { animatedChild } };

            Assert.That(adapter.TryAttach(target, out var session, out var diagnostic), Is.True, diagnostic);
            var clockSession = session!;
            using (clockSession)
            {
                source.Pulse(TimeSpan.Zero);
                var animationTask = CreateOpacityAnimation().RunAsync(animatedChild);

                clockSession.Pause();
                source.Pulse(TimeSpan.FromMilliseconds(100));
                Assert.That(animatedChild.Opacity, Is.EqualTo(0d).Within(0.001d));

                clockSession.Advance(TimeSpan.FromMilliseconds(250));
                Assert.Multiple(() =>
                {
                    Assert.That(clockSession.CurrentTime, Is.EqualTo(TimeSpan.FromMilliseconds(250)));
                    Assert.That(animatedChild.Opacity, Is.EqualTo(0.25d).Within(0.001d));
                });

                clockSession.Advance(TimeSpan.FromMilliseconds(250));
                Assert.That(animatedChild.Opacity, Is.EqualTo(0.5d).Within(0.001d));

                clockSession.Reset();
                Assert.Multiple(() =>
                {
                    Assert.That(clockSession.CurrentTime, Is.EqualTo(TimeSpan.Zero));
                    Assert.That(animatedChild.Opacity, Is.EqualTo(0d).Within(0.001d));
                });

                clockSession.Resume();
                source.Pulse(TimeSpan.FromSeconds(1));
                source.Pulse(TimeSpan.FromMilliseconds(1_250));
                Assert.That(animatedChild.Opacity, Is.EqualTo(0.25d).Within(0.001d));

                clockSession.Pause();
                source.Pulse(TimeSpan.FromMilliseconds(1_500));
                source.Pulse(TimeSpan.FromMilliseconds(1_750));
                Assert.That(animatedChild.Opacity, Is.EqualTo(0.25d).Within(0.001d));

                clockSession.Dispose();
                source.Pulse(TimeSpan.FromSeconds(2));
                source.Pulse(TimeSpan.FromMilliseconds(2_250));
                Assert.That(
                    animatedChild.Opacity,
                    Is.EqualTo(0.5d).Within(0.001d),
                    "Animations that captured the diagnostic clock must resume after the property override is released.");

                source.Pulse(TimeSpan.FromMilliseconds(2_750));
                Assert.Multiple(() =>
                {
                    Assert.That(animatedChild.Opacity, Is.EqualTo(1d).Within(0.001d));
                    Assert.That(animationTask.IsCompletedSuccessfully, Is.True);
                });
            }

            Assert.That(source.SubscriberCount, Is.Zero);
        });
    }

    [Test]
    public void DisposingAClockSessionRestoresThePreviousClockValue()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var firstSource = new ManualAnimationTimeSource();
            var secondSource = new ManualAnimationTimeSource();
            var target = new Border();
            AvaloniaProperty? clockProperty = null;
            target.PropertyChanged += (_, change) =>
            {
                if (change.Property.Name == "Clock")
                {
                    clockProperty = change.Property;
                }
            };

            var firstAdapter = new AvaloniaAnimationClockAdapter(SupportedCompatibility, () => firstSource);
            var secondAdapter = new AvaloniaAnimationClockAdapter(SupportedCompatibility, () => secondSource);
            Assert.That(firstAdapter.TryAttach(target, out var firstSession, out var firstDiagnostic), Is.True, firstDiagnostic);
            Assert.That(clockProperty, Is.Not.Null);
            var firstClock = target.GetValue(clockProperty!);

            Assert.That(secondAdapter.TryAttach(target, out var secondSession, out var secondDiagnostic), Is.True, secondDiagnostic);
            Assert.That(target.GetValue(clockProperty!), Is.Not.SameAs(firstClock));

            secondSession!.Dispose();
            Assert.That(target.GetValue(clockProperty!), Is.SameAs(firstClock));

            firstSession!.Dispose();
            Assert.Multiple(() =>
            {
                Assert.That(target.GetValue(clockProperty!), Is.Null);
                Assert.That(firstSource.SubscriberCount, Is.Zero);
                Assert.That(secondSource.SubscriberCount, Is.Zero);
            });
        });
    }

    [Test]
    public void UnsupportedApiShapeReturnsDiagnosticWithoutCreatingClock()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var sourceFactoryCalled = false;
            var adapter = new AvaloniaAnimationClockAdapter(
                new AnimationClockCompatibility(false, "ClockProperty shape is unsupported."),
                () =>
                {
                    sourceFactoryCalled = true;
                    return new ManualAnimationTimeSource();
                });

            var attached = adapter.TryAttach(new Border(), out var session, out var diagnostic);

            Assert.Multiple(() =>
            {
                Assert.That(attached, Is.False);
                Assert.That(session, Is.Null);
                Assert.That(sourceFactoryCalled, Is.False);
                Assert.That(diagnostic, Does.Contain("unsupported"));
            });
        });
    }

    private static Animation CreateOpacityAnimation()
    {
        return new Animation
        {
            Duration = TimeSpan.FromSeconds(1),
            Easing = new LinearEasing(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters = { new Setter(Visual.OpacityProperty, 0d) },
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter(Visual.OpacityProperty, 1d) },
                },
            },
        };
    }

    private sealed class ManualAnimationTimeSource : IAnimationTimeSource
    {
        private readonly List<Action<TimeSpan>> _subscribers = [];

        public int SubscriberCount => _subscribers.Count;

        public IDisposable Subscribe(Action<TimeSpan> tick)
        {
            _subscribers.Add(tick);
            return new ActionDisposable(() => _subscribers.Remove(tick));
        }

        public void Pulse(TimeSpan time)
        {
            foreach (var subscriber in _subscribers.ToArray())
            {
                subscriber(time);
            }
        }
    }

    private sealed class ActionDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose()
        {
            Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }
}
