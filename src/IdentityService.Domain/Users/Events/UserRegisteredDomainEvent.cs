using IdentityService.Domain.Common;

namespace IdentityService.Domain.Users.Events;

public sealed record UserRegisteredDomainEvent(UserId UserId, string Email) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
