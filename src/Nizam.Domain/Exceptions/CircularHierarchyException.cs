namespace Nizam.Domain.Exceptions;

public sealed class CircularHierarchyException : DomainException
{
    public Guid EntityId { get; }
    public Guid? ParentId { get; }

    public CircularHierarchyException(Guid entityId, Guid? parentId, string? message = null)
        : base(
            message ?? $"Circular hierarchy detected when assigning parent '{parentId}' to entity '{entityId}'.",
            "circular_hierarchy")
    {
        EntityId = entityId;
        ParentId = parentId;
    }
}
