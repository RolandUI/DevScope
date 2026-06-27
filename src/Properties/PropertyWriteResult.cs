using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Properties;

internal readonly record struct PropertyWriteResult(
    bool IsSuccess,
    object? WrittenValue,
    Exception? Exception,
    string? ErrorMessage)
{
    public static PropertyWriteResult Success(object? writtenValue)
    {
        return new PropertyWriteResult(true, writtenValue, null, null);
    }

    public static PropertyWriteResult Failure(Exception exception, string message)
    {
        return new PropertyWriteResult(false, null, exception, message);
    }
}
