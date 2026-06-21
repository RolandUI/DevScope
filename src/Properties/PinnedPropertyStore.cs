namespace ClassicDiagnostics.Avalonia.Properties;

internal sealed class PinnedPropertyStore : IPinnedPropertyStore
{
    private readonly HashSet<string> _keys = new();

    public bool Contains(string key)
    {
        return _keys.Contains(key);
    }

    public bool Pin(string key)
    {
        _keys.Add(key);
        return true;
    }

    public bool Toggle(string key)
    {
        if (_keys.Add(key))
        {
            return true;
        }

        _keys.Remove(key);
        return false;
    }

    public bool Unpin(string key)
    {
        _keys.Remove(key);
        return false;
    }
}
