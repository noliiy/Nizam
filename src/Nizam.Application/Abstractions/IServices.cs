namespace Nizam.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    Guid? OrganizationId { get; }
    bool IsAuthenticated { get; }
}

public interface IDateTime
{
    DateTime UtcNow { get; }
}

public interface IAuditService
{
    Task WriteAsync(
        Guid organizationId,
        string entityType,
        string entityId,
        string action,
        string? oldValue = null,
        string? newValue = null,
        Guid? projectId = null,
        CancellationToken cancellationToken = default);
}

public interface IOutboxWriter
{
    Task EnqueueAsync(string type, object payload, CancellationToken cancellationToken = default);
}

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email, Guid? organizationId = null);
}
