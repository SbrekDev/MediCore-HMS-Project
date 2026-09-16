using MediCore.Domain.Errors;

namespace MediCore.Domain.Common;

public sealed class Result<T>
{
    private readonly T? _value;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public DomainError Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.", new InvalidOperationException(Error.Message));

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        Error = default!;
    }

    private Result(DomainError error)
    {
        IsSuccess = false;
        _value = default;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(DomainError error) => new(error);
}
