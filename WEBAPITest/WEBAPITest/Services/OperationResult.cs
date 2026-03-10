namespace WEBAPITest.Services;

public sealed record OperationResult(bool Success, string Message)
{
    public static OperationResult Ok(string message = "OK") => new(true, message);
    public static OperationResult Fail(string message) => new(false, message);
}