namespace IdentityService.Domain.Common;

// The Application layer only loads/persists aggregate roots (one repository per
// aggregate). Raised events are drained and dispatched by Infrastructure after a
// successful commit — this is the hook a distributed system uses to react to
// identity changes (e.g. provisioning a profile in another service on UserRegistered).
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot() { }
    protected AggregateRoot(TId id) : base(id) { }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
