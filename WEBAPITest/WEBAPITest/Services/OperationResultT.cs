namespace WEBAPITest.Services;

public sealed record OperationResult<T>(bool Success, string Message, T? Data)
{
    public static OperationResult<T> Ok(T data, string message = "OK") => new(true, message, data);
    public static OperationResult<T> Fail(string message) => new(false, message, default);
}