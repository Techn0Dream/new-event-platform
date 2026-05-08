namespace TechTrek.Domain.Primitives;

/// <summary>
/// Auditable entity: tracks creation, modification, and soft-delete timestamps + actor IDs.
/// Implements soft delete via DeletedAt timestamp - rows are never physically removed.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; protected set; }

    public Guid? CreatedBy { get; protected set; }
    public Guid? UpdatedBy { get; protected set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public void SoftDelete(Guid deletedBy)
    {
        DeletedAt = DateTime.UtcNow;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    protected void Touch(Guid? updatedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        if (updatedBy.HasValue) UpdatedBy = updatedBy;
    }
}
