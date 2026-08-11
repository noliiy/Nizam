using System.Text.Json;
using Nizam.Application.Abstractions;
using Nizam.Domain.Entities;
using Nizam.Infrastructure.Persistence;

namespace Nizam.Infrastructure.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly NizamDbContext _db;
    private readonly IDateTime _clock;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OutboxWriter(NizamDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task EnqueueAsync(string type, object payload, CancellationToken cancellationToken = default)
    {
        _db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = JsonSerializer.Serialize(payload, JsonOptions),
            CreatedAt = _clock.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AuditService : IAuditService
{
    private readonly NizamDbContext _db;
    private readonly IDateTime _clock;
    private readonly ICurrentUser _currentUser;

    public AuditService(NizamDbContext db, IDateTime clock, ICurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task WriteAsync(
        Guid organizationId,
        string entityType,
        string entityId,
        string action,
        string? oldValue = null,
        string? newValue = null,
        Guid? projectId = null,
        CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = projectId,
            UserId = _currentUser.UserId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            Timestamp = _clock.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
