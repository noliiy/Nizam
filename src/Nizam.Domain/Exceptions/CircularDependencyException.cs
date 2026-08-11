namespace Nizam.Domain.Exceptions;

public sealed class CircularDependencyException : DomainException
{
    public Guid? PredecessorId { get; }
    public Guid? SuccessorId { get; }

    public CircularDependencyException(string message, Guid? predecessorId = null, Guid? successorId = null)
        : base(message, "circular_dependency")
    {
        PredecessorId = predecessorId;
        SuccessorId = successorId;
    }

    public CircularDependencyException(Guid predecessorId, Guid successorId)
        : this(
            $"Circular dependency detected between activities '{predecessorId}' and '{successorId}'.",
            predecessorId,
            successorId)
    {
    }
}
