namespace Nizam.Domain.Exceptions;

public class DomainException : Exception
{
    public string? Code { get; }

    public DomainException(string message, string? code = null)
        : base(message)
    {
        Code = code;
    }

    public DomainException(string message, Exception innerException, string? code = null)
        : base(message, innerException)
    {
        Code = code;
    }
}
