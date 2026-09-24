namespace IdentityService.Domain.Common;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    protected Entity() { }
    protected Entity(TId id) => Id = id;

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);
    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();
    public static bool operator ==(Entity<TId>? a, Entity<TId>? b) => a is null && b is null || (a is not null && a.Equals(b));
    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !(a == b);
}
