namespace ClassicDiagnostics.Avalonia.Properties;

internal interface IPinnedPropertyStore
{
    bool Contains(string key);

    bool Pin(string key);

    bool Toggle(string key);

    bool Unpin(string key);
}
