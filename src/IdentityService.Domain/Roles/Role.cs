namespace IdentityService.Domain.Roles;

// Kept intentionally simple (name-based, no separate aggregate/repository) since
// this service owns authentication, not a full authorization/permissions model —
// downstream services are expected to make their own authorization decisions
// based on the roles/claims embedded in the JWT.
public static class Role
{
    public const string User = "User";
    public const string Admin = "Admin";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { User, Admin };

    public static bool IsValid(string role) => All.Contains(role);
}
