namespace InventoryApp;

public sealed record Error(string Code, string Message);

public sealed class Result<T>
{
    private readonly T? value;
    public bool IsSuccess { get; }
    public Error? Error { get; }
    public T Value => IsSuccess ? value! : throw new InvalidOperationException("A failed result has no value.");
    private Result(T value) { this.value = value; IsSuccess = true; }
    private Result(Error error) { Error = error; }
    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string code, string message) => new(new Error(code, message));
}
