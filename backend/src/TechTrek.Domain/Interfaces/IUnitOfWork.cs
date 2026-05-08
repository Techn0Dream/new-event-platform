namespace TechTrek.Domain.Interfaces;

/// <summary>
/// Unit of Work - wraps multiple repository operations in a single transaction.
/// 
/// WHY: When VerifyRiddleCommand fires, we need to:
///   1. Unlock the stage in the Team aggregate
///   2. Write an OutboxMessage for the SignalR broadcast
///   3. Write an AuditLog entry
/// All three must succeed or all three must fail atomically.
/// 
/// UoW ensures all repository changes are committed in one DB transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
