namespace Nizam.Application.Common;

public sealed class ValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : base(string.Join(" ", failures.Select(f => f.ErrorMessage)))
    {
        Errors = failures.Select(f => f.ErrorMessage).ToList();
    }

    public ValidationException(string message) : base(message)
    {
        Errors = [message];
    }
}

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
