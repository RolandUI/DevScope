using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Interactivity;

namespace ClassicDiagnostics.Avalonia.Models;

internal class FiredEvent : INotifyPropertyChanged
{
    private readonly RoutedEventArgs _eventArgs;
    private readonly RoutedEvent? _originalEvent;

    public FiredEvent(RoutedEventArgs eventArgs, EventChainLink originator, DateTime triggerTime)
    {
        _eventArgs = eventArgs ?? throw new ArgumentNullException(nameof(eventArgs));
        Originator = originator ?? throw new ArgumentNullException(nameof(originator));
        _originalEvent = _eventArgs.RoutedEvent;
        AddToChain(originator);
        TriggerTime = triggerTime;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DateTime TriggerTime { get; }

    public RoutedEvent Event => _originalEvent!;

    public bool IsHandled => HandledBy?.Handled == true;

    public ObservableCollection<EventChainLink> EventChain { get; } = new();

    public string DisplayText
    {
        get
        {
            if (IsHandled)
            {
                return $"{Event.Name} on {Originator.HandlerName};" + Environment.NewLine +
                    $"strategies: {Event.RoutingStrategies}; handled by: {HandledBy!.HandlerName}";
            }

            return $"{Event.Name} on {Originator.HandlerName}; strategies: {Event.RoutingStrategies}";
        }
    }

    public EventChainLink Originator { get; }

    public EventChainLink? HandledBy
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsHandled));
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
    }

    public bool IsPartOfSameEventChain(RoutedEventArgs e)
    {
        // Avalonia can reuse RoutedEventArgs instances for distinct routed events,
        // so the original event identity is part of this chain check.
        return e == _eventArgs && e.RoutedEvent == _originalEvent;
    }

    public void AddToChain(EventChainLink link)
    {
        if (EventChain.Count > 0)
        {
            var prevLink = EventChain[^1];

            if (prevLink.Route != link.Route)
            {
                link.BeginsNewRoute = true;
            }
        }

        EventChain.Add(link);

        if (HandledBy == null && link.Handled)
        {
            HandledBy = link;
        }
    }

    private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
