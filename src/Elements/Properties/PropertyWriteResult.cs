namespace RolandUI.DevScope.Elements.Properties;

internal readonly record struct PropertyWriteResult(
    bool IsSuccess,
    object? WrittenValue,
    Exception? Exception,
    string? ErrorMessage
)
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