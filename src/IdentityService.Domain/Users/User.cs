using IdentityService.Domain.Common;
using IdentityService.Domain.Roles;
using IdentityService.Domain.Users.Events;

namespace IdentityService.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<string> _roles = new();
    private readonly List<RefreshToken> _refreshTokens = new();

    public Email Email { get; private set; } = default!;
    public HashedPassword Password { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public bool IsEmailConfirmed { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedOutUntilUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? LastLoginOnUtc { get; private set; }

    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public bool IsLockedOut => LockedOutUntilUtc is not null && LockedOutUntilUtc > DateTime.UtcNow;

    private User() { }

    public static User Register(Email email, HashedPassword password, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("Last name is required.");

        var user = new User
        {
            Id = UserId.New(),
            Email = email,
            Password = password,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            CreatedOnUtc = DateTime.UtcNow,
            IsEmailConfirmed = false,
            IsActive = true
        };

        user._roles.Add(Role.User);
        user.Raise(new UserRegisteredDomainEvent(user.Id, user.Email.Value));
        return user;
    }

    public void ConfirmEmail() => IsEmailConfirmed = true;

    public void Deactivate() => IsActive = false;

    public void AssignRole(string role)
    {
        if (!Role.IsValid(role)) throw new DomainException($"'{role}' is not a recognized role.");
        if (!_roles.Contains(role)) _roles.Add(role);
    }

    // Called by the Login use case after credential verification fails.
    // Kept on the aggregate (not the handler) so the lockout invariant can
    // never be bypassed regardless of which caller checks credentials.
    public void RegisterFailedLoginAttempt()
    {
        if (IsLockedOut) return;

        FailedLoginAttempts++;
        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockedOutUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
            Raise(new UserLockedOutDomainEvent(Id, LockedOutUntilUtc.Value));
        }
    }

    public void RegisterSuccessfulLogin(string? ip)
    {
        FailedLoginAttempts = 0;
        LockedOutUntilUtc = null;
        LastLoginOnUtc = DateTime.UtcNow;
        Raise(new UserLoggedInDomainEvent(Id, ip));
    }

    public void ChangePassword(HashedPassword newPassword)
    {
        Password = newPassword;
        // Changing a password invalidates every outstanding session — a
        // deliberate security default for a distributed system where a
        // compromised token could otherwise keep working elsewhere.
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
            token.Revoke();

        Raise(new PasswordChangedDomainEvent(Id));
    }

    public RefreshToken IssueRefreshToken(string tokenHash, TimeSpan lifetime, string? ip)
    {
        var token = RefreshToken.Issue(Id, tokenHash, lifetime, ip);
        _refreshTokens.Add(token);
        return token;
    }

    public void RevokeRefreshToken(RefreshToken token, Guid? replacedByTokenId = null)
    {
        if (!_refreshTokens.Contains(token))
            throw new DomainException("Refresh token does not belong to this user.");

        token.Revoke(replacedByTokenId);
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
            token.Revoke();
    }
}
