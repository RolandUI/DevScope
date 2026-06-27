using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

internal sealed class PropertyExplorerViewModel : ReactiveViewModelBase
{
    private readonly PropertyColumnWidthStore _widthStore = new();
    private readonly IPropertyColumnFactory _columnFactory;
    private PropertyNavigationEntry? _rootEntry;

    public PropertyExplorerViewModel(IPropertyColumnFactory columnFactory)
    {
        _columnFactory = columnFactory;
        DrillIn = new PropertyDrillInViewModel(columnFactory, _widthStore);
        Columns = new PropertyColumnsViewModel(columnFactory, _widthStore);
    }

    public PropertyNavigationMode NavigationMode
    {
        get;
        set
        {
            var oldMode = field;
            if (SetProperty(ref field, value))
            {
                ConvertMode(oldMode, value);
            }
        }
    } = PropertyNavigationMode.DrillIn;

    public PropertyDrillInViewModel DrillIn { get; }

    public PropertyColumnsViewModel Columns { get; }

    public ReactiveViewModelBase CurrentMode => NavigationMode == PropertyNavigationMode.Columns ? Columns : DrillIn;

    public ObjectPropertiesColumnViewModel? RootObjectColumn =>
        NavigationMode == PropertyNavigationMode.Columns ? Columns.RootObjectColumn : DrillIn.RootObjectColumn;

    public void SetMode(PropertyNavigationMode mode)
    {
        NavigationMode = mode;
    }

    public void OpenRoot(object target, string title)
    {
        _rootEntry = new PropertyNavigationEntry(target, title, title);
        DrillIn.OpenRoot(target, title);
        Columns.OpenRoot(target, title);
        RaisePropertyChanged(nameof(CurrentMode));
    }

    public void SelectPropertyInRoot(AvaloniaProperty property)
    {
        RootObjectColumn?.SelectProperty(property);
    }

    public void OpenSelectedFromRoot()
    {
        if (RootObjectColumn?.SelectedProperty is { } property)
        {
            if (NavigationMode == PropertyNavigationMode.Columns && Columns.RootColumn is { } rootColumn)
            {
                Columns.OpenFrom(rootColumn, property);
            }
            else
            {
                DrillIn.OpenFrom(property);
            }
        }
    }

    public void RefreshViews()
    {
        DrillIn.RefreshViews();
        Columns.RefreshViews();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DrillIn.Dispose();
            Columns.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ConvertMode(PropertyNavigationMode oldMode, PropertyNavigationMode newMode)
    {
        if (oldMode == PropertyNavigationMode.Columns && newMode == PropertyNavigationMode.DrillIn)
        {
            DrillIn.LoadPath(Columns.GetPath());
        }
        else if (oldMode == PropertyNavigationMode.DrillIn && newMode == PropertyNavigationMode.Columns)
        {
            Columns.LoadPath(DrillIn.GetPath());
        }
        else if (_rootEntry is { } rootEntry)
        {
            DrillIn.LoadPath([rootEntry]);
            Columns.LoadPath([rootEntry]);
        }

        RaisePropertyChanged(nameof(CurrentMode));
        RaisePropertyChanged(nameof(RootObjectColumn));
    }
}