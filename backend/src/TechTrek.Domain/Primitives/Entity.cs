namespace TechTrek.Domain.Primitives;

/// <summary>
/// Base entity with UUID public ID. Internal DB primary key is separate.
/// Frontend NEVER sees int/long keys - only Guid PublicId.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity() { }

    public Guid Id { get; protected init; } = Guid.NewGuid();

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);
    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) =>
        left is not null && right is not null && left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
