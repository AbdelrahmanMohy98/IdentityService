namespace IdentityService.Domain.Users;

// Strongly-typed id: makes UserId vs Guid mix-ups a compile error, and gives
// other services in the distributed system an unambiguous type when the id
// crosses a boundary (event payloads, JWT "sub" claim, API contracts).
public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
