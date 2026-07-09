using RolandUI.DevScope.Elements.Properties.ViewModels;

namespace RolandUI.DevScope.Elements.Properties.Services;

internal sealed class PropertyColumnWidthStore
{
    private readonly Dictionary<PropertyColumnKey, double> _widths = new();

    public bool TryGet(PropertyColumnKey key, out double width)
    {
        return _widths.TryGetValue(key, out width);
    }

    public void Set(PropertyColumnKey key, double width)
    {
        _widths[key] = width;
    }
}