using IdentityService.Domain.Common;

namespace IdentityService.Domain.Users.Events;

public sealed record UserLockedOutDomainEvent(UserId UserId, DateTime LockedUntilUtc) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
