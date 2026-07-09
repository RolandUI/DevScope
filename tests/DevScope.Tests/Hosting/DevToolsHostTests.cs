using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using RolandUI.DevScope.Extensions;
using RolandUI.DevScope.Hosting;
using RolandUI.DevScope.Shell;
using RolandUI.DevScope.Views.Shell;
using Application = Avalonia.Application;

namespace RolandUI.DevScope.Tests.Hosting;

internal sealed class DevToolsHostTests
{
    [Test]
    public void SessionRegistrySharesOneSubscriptionOwnerPerApplication()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var registry = new DevToolsApplicationSessionRegistry();
            var application = Application.Current!;
            var createdSessions = new List<RecordingDisposable>();

            IDisposable CreateSession()
            {
                var session = new RecordingDisposable();
                createdSessions.Add(session);
                return session;
            }

            var firstLease = registry.Acquire(application, CreateSession);
            var secondLease = registry.Acquire(application, CreateSession);

            Assert.That(createdSessions, Has.Count.EqualTo(1));

            firstLease.Dispose();
            firstLease.Dispose();
            Assert.That(createdSessions[0].DisposeCount, Is.Zero);

            secondLease.Dispose();
            Assert.That(createdSessions[0].DisposeCount, Is.EqualTo(1));

            using var thirdLease = registry.Acquire(application, CreateSession);
            Assert.That(createdSessions, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void HostManagerReusesOneSurfaceAndDisposesItDeterministically()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var factory = new RecordingSurfaceHostFactory();
            var manager = new DevToolsHostManager(
                new TestRootSource(DevToolsHostKind.DesktopWindow),
                new DevToolsOptions(),
                Application.Current!,
                factory);

            manager.ShowOrActivate();
            manager.ShowOrActivate();

            Assert.Multiple(() =>
            {
                Assert.That(factory.CreateCount, Is.EqualTo(1));
                Assert.That(factory.Hosts[0].ShowOrActivateCount, Is.EqualTo(2));
                Assert.That(manager.HasActiveHost, Is.True);
                Assert.That(manager.ActiveKind, Is.EqualTo(DevToolsHostKind.DesktopWindow));
            });

            factory.Hosts[0].RaiseClosed();

            Assert.Multiple(() =>
            {
                Assert.That(manager.HasActiveHost, Is.False);
                Assert.That(factory.Hosts[0].DisposeCount, Is.EqualTo(1));
            });

            manager.ShowOrActivate();
            manager.Dispose();
            manager.Dispose();

            Assert.Multiple(() =>
            {
                Assert.That(factory.CreateCount, Is.EqualTo(2));
                Assert.That(factory.Hosts[1].CloseCount, Is.EqualTo(1));
                Assert.That(factory.Hosts[1].DisposeCount, Is.EqualTo(1));
                Assert.That(manager.HasActiveHost, Is.False);
                Assert.That(() => manager.ShowOrActivate(), Throws.TypeOf<ObjectDisposedException>());
            });
        });
    }

    [Test]
    public void FactorySelectsEmbeddedHostOnlyForSingleViewLifetime()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var application = Application.Current!;
            var lifetime = new TestSingleViewLifetime { MainView = new Border() };
            var singleViewSource = new SingleViewApplicationRootSource(application, lifetime);

            using var embedded = DevToolsSurfaceHostFactory.Instance.Create(
                singleViewSource,
                new DevToolsOptions(),
                application);
            using var desktop = DevToolsSurfaceHostFactory.Instance.Create(
                new TestRootSource(DevToolsHostKind.DesktopWindow),
                new DevToolsOptions(),
                application);

            desktop.ShowOrActivate(null);

            Assert.Multiple(() =>
            {
                Assert.That(embedded, Is.TypeOf<EmbeddedDevToolsSurfaceHost>());
                Assert.That(embedded.Kind, Is.EqualTo(DevToolsHostKind.EmbeddedSingleView));
                Assert.That(desktop, Is.TypeOf<DesktopDevToolsSurfaceHost>());
                Assert.That(desktop.Kind, Is.EqualTo(DevToolsHostKind.DesktopWindow));
            });
        });
    }

    [Test]
    public void EmbeddedHostFallbackRestoresOriginalMainViewAndOwnedState()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var application = Application.Current!;
            var originalMainView = new Border { Child = new TextBlock { Text = "Application" } };
            var lifetime = new TestSingleViewLifetime { MainView = originalMainView };
            var rootSource = new SingleViewApplicationRootSource(application, lifetime);
            using var host = new EmbeddedDevToolsSurfaceHost(rootSource, new DevToolsOptions(), application);
            var closedCount = 0;
            host.Closed += (_, _) => closedCount++;

            host.ShowOrActivate(null);

            var fallbackRoot = lifetime.MainView as Grid;
            Assert.That(fallbackRoot, Is.Not.Null);
            var embeddedView = fallbackRoot!.Children.OfType<EmbeddedDevToolsView>().Single();
            Assert.Multiple(() =>
            {
                Assert.That(fallbackRoot.Children, Has.Count.EqualTo(2));
                Assert.That(fallbackRoot.Children[0], Is.SameAs(originalMainView));
                Assert.That(fallbackRoot.Children, Has.None.TypeOf<Window>());
                Assert.That(embeddedView.DataContext, Is.Not.Null);
                Assert.That(closedCount, Is.Zero);
            });

            host.ShowOrActivate(null);

            Assert.Multiple(() =>
            {
                Assert.That(lifetime.MainView, Is.SameAs(originalMainView));
                Assert.That(originalMainView.Parent, Is.Null);
                Assert.That(embeddedView.DataContext, Is.Null);
                Assert.That(closedCount, Is.EqualTo(1));
            });
        });
    }

    [Test]
    public void EmbeddedHostUsesAttachedOverlayAndRemovesItOnClose()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var application = Application.Current!;
            var originalMainView = new Border { Child = new TextBlock { Text = "Application" } };
            var window = new Window
            {
                Width = 640,
                Height = 480,
                Content = originalMainView,
            };
            window.Show();

            try
            {
                var overlayLayer = OverlayLayer.GetOverlayLayer(originalMainView);
                Assert.That(overlayLayer, Is.Not.Null);

                var lifetime = new TestSingleViewLifetime { MainView = originalMainView };
                var rootSource = new SingleViewApplicationRootSource(application, lifetime);
                using var host = new EmbeddedDevToolsSurfaceHost(rootSource, new DevToolsOptions(), application);

                host.ShowOrActivate(null);

                var embeddedView = overlayLayer!.Children.OfType<EmbeddedDevToolsView>().Single();
                Dispatcher.UIThread.RunJobs();
                Assert.Multiple(() =>
                {
                    Assert.That(lifetime.MainView, Is.SameAs(originalMainView));
                    Assert.That(TopLevel.GetTopLevel(embeddedView), Is.SameAs(window));
                    Assert.That(embeddedView.Bounds.Size, Is.EqualTo(overlayLayer.Bounds.Size));
                    Assert.That(embeddedView.DoesBelongToDevTool(), Is.True);
                });

                host.Dispose();

                Assert.Multiple(() =>
                {
                    Assert.That(overlayLayer.Children, Has.None.SameAs(embeddedView));
                    Assert.That(embeddedView.DataContext, Is.Null);
                    Assert.That(lifetime.MainView, Is.SameAs(originalMainView));
                });
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Test]
    public void MainViewModelCloseInvokesSurfaceCallback()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var closeCount = 0;
            using var viewModel = new MainViewModel(new Border(), () => closeCount++);

            viewModel.Close();

            Assert.That(closeCount, Is.EqualTo(1));
        });
    }

    private sealed class TestSingleViewLifetime : IDevToolsSingleViewLifetime
    {
        public Control? MainView { get; set; }

        public TopLevel? TopLevel { get; set; }
    }

    private sealed class TestRootSource(DevToolsHostKind hostKind) : IDevToolsRootSource
    {
        public DevToolsHostKind HostKind => hostKind;

        public IReadOnlyList<TopLevel> Items { get; } = [];
    }

    private sealed class RecordingSurfaceHostFactory : IDevToolsSurfaceHostFactory
    {
        public int CreateCount { get; private set; }

        public List<RecordingSurfaceHost> Hosts { get; } = [];

        public IDevToolsSurfaceHost Create(
            IDevToolsRootSource rootSource,
            DevToolsOptions options,
            Application application)
        {
            CreateCount++;
            var host = new RecordingSurfaceHost();
            Hosts.Add(host);
            return host;
        }
    }

    private sealed class RecordingSurfaceHost : IDevToolsSurfaceHost
    {
        public event EventHandler? Closed;

        public DevToolsHostKind Kind => DevToolsHostKind.DesktopWindow;

        public int ShowOrActivateCount { get; private set; }

        public int CloseCount { get; private set; }

        public int DisposeCount { get; private set; }

        public void ShowOrActivate(Control? focusedControl)
        {
            ShowOrActivateCount++;
        }

        public void Close()
        {
            CloseCount++;
        }

        public void Dispose()
        {
            DisposeCount++;
        }

        public void RaiseClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class RecordingDisposable : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
