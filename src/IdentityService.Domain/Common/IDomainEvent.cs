using MediatR;

namespace IdentityService.Domain.Common;

// Domain events implement INotification so the same MediatR pipeline used for
// commands/queries in the Application layer can dispatch them (published from
// Infrastructure right after SaveChanges, inside the same unit of work). Other
// services in the distributed system consume these indirectly via the Outbox
// (see Infrastructure/Outbox) rather than the domain depending on any bus.
public interface IDomainEvent : INotification
{
    DateTime OccurredOnUtc { get; }
}
