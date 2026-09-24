namespace IdentityService.Domain.Common;

// Reserved for broken invariants only. Ordinary business failures ("email
// already registered", "invalid credentials") are Result failures in the
// Application layer, not exceptions — see Common/Models/Result.cs.
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
