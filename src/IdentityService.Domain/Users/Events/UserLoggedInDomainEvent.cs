using IdentityService.Domain.Common;

namespace IdentityService.Domain.Users.Events;

public sealed record UserLoggedInDomainEvent(UserId UserId, string? Ip) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
