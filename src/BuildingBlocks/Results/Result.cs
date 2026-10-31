namespace CalorieTracker.BuildingBlocks.Results;

public class Result
{
    protected Result(Error error)
    {
        Error = error;
    }

    public Error Error { get; }

    public bool IsSuccess => Error == Error.None;

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(Error.None);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, Error.None);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    public static Result<TValue> Failure<TValue>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue>(default, error);
    }
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, Error error)
        : base(error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");
}
