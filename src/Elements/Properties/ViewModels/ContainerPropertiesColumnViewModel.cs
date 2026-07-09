using System.Collections;
using System.Collections.ObjectModel;
using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Elements.Properties.ViewModels;

internal sealed class ContainerPropertiesColumnViewModel : ReactiveViewModelBase, IPropertyColumnContentViewModel
{
    private const int PageSize = 100;

    private IEnumerator? _enumerator;
    private int _nextIndex;

    public ContainerPropertiesColumnViewModel(
        object target,
        string title,
        string path,
        PropertyValueDescriptorKind kind)
    {
        Target = target;
        Title = title;
        Path = path;
        Kind = kind;

        Filter.RefreshFilter += HandleFilterRefreshFilter;
        Refresh();
    }

    public event EventHandler<PropertyContainerItemViewModel?>? SelectedItemChanged;

    public object Target { get; }

    public string Title { get; }

    public string Path { get; }

    public PropertyValueDescriptorKind Kind { get; }

    public FilterViewModel Filter { get; } = new();

    public ObservableCollection<PropertyContainerItemViewModel> Items { get; } = [];

    public bool ShowsIndex => Kind is PropertyValueDescriptorKind.Array
        or PropertyValueDescriptorKind.List
        or PropertyValueDescriptorKind.Enumerable;

    public bool ShowsKey => Kind == PropertyValueDescriptorKind.Dictionary;

    public bool CanShowMore
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string Status
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    public PropertyContainerItemViewModel? SelectedItem
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SelectedItemChanged?.Invoke(this, value);
            }
        }
    }

    public void Refresh()
    {
        SelectedItem = null;
        Items.Clear();
        _nextIndex = 0;
        _enumerator = CreateEnumerator();
        CanShowMore = _enumerator is not null;

        if (Kind != PropertyValueDescriptorKind.Enumerable)
        {
            ShowMore();
        }
        else
        {
            UpdateStatus();
        }
    }

    public void ShowMore()
    {
        if (_enumerator is null)
        {
            CanShowMore = false;
            UpdateStatus();
            return;
        }

        var acceptedCount = 0;

        while (acceptedCount < PageSize && _enumerator.MoveNext())
        {
            var item = CreateItem(_enumerator.Current, _nextIndex);
            _nextIndex++;

            if (!Filter.Filter(item.Name)
                && !Filter.Filter(item.ValueText)
                && !Filter.Filter(item.Type))
            {
                continue;
            }

            Items.Add(item);
            acceptedCount++;
        }

        CanShowMore = acceptedCount == PageSize;

        if (!CanShowMore)
        {
            (_enumerator as IDisposable)?.Dispose();
            _enumerator = null;
        }

        UpdateStatus();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Filter.RefreshFilter -= HandleFilterRefreshFilter;
            (_enumerator as IDisposable)?.Dispose();
            _enumerator = null;
        }

        base.Dispose(disposing);
    }

    private IEnumerator? CreateEnumerator()
    {
        return Kind switch
        {
            PropertyValueDescriptorKind.Array when Target is Array array => array.GetEnumerator(),
            PropertyValueDescriptorKind.List when Target is IList list => list.GetEnumerator(),
            PropertyValueDescriptorKind.Dictionary when Target is IDictionary dictionary => dictionary.GetEnumerator(),
            PropertyValueDescriptorKind.Enumerable when Target is IEnumerable enumerable => enumerable.GetEnumerator(),
            _ => null,
        };
    }

    private PropertyContainerItemViewModel CreateItem(object? entry, int index)
    {
        if (Kind == PropertyValueDescriptorKind.Dictionary)
        {
            var (key, value) = GetDictionaryEntry(entry);
            return new PropertyContainerItemViewModel($"[{key}]", key, value, index);
        }

        return new PropertyContainerItemViewModel($"[{index}]", null, entry, index);
    }

    private void HandleFilterRefreshFilter(object? sender, EventArgs args)
    {
        Refresh();
    }

    private void UpdateStatus()
    {
        Status = Kind == PropertyValueDescriptorKind.Enumerable && Items.Count == 0 && CanShowMore
            ? "Enumerable is not expanded."
            : $"{Items.Count} item{(Items.Count == 1 ? string.Empty : "s")} loaded.";
    }

    private static (object? Key, object? Value) GetDictionaryEntry(object? entry)
    {
        if (entry is DictionaryEntry dictionaryEntry)
        {
            return (dictionaryEntry.Key, dictionaryEntry.Value);
        }

        if (entry is null)
        {
            return (null, null);
        }

        var entryType = entry.GetType();
        var key = entryType.GetProperty("Key")?.GetValue(entry);
        var value = entryType.GetProperty("Value")?.GetValue(entry);

        return (key, value);
    }
}
