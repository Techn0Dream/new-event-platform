using MediatR;

namespace TechTrek.Domain.Primitives;

/// <summary>
/// Aggregate root: owns domain events and enforces consistency boundaries.
/// Only aggregate roots are fetched directly from repositories.
/// Children (TeamMember, StageProgress) are always accessed through their aggregate root.
/// </summary>
public abstract class AggregateRoot : AuditableEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// Marker interface for domain events.
/// Domain events are raised within aggregate methods, dispatched AFTER successful DB commit (via Outbox).
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
