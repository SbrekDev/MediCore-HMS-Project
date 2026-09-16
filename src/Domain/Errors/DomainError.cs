namespace MediCore.Domain.Errors;

public sealed class DomainError
{
    public string Code { get; }
    public string Message { get; }

    private DomainError(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static DomainError NotFound(string code, string message) => new(code, message);
    public static DomainError Validation(string code, string message) => new(code, message);
    public static DomainError Conflict(string code, string message) => new(code, message);
    public static DomainError Unauthorized(string code, string message) => new(code, message);
    public static DomainError Forbidden(string code, string message) => new(code, message);
}
