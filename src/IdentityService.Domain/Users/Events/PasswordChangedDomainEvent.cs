using IdentityService.Domain.Common;

namespace IdentityService.Domain.Users.Events;

public sealed record PasswordChangedDomainEvent(UserId UserId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
