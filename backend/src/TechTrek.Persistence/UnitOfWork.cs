using Microsoft.EntityFrameworkCore.Storage;
using TechTrek.Domain.Interfaces;

namespace TechTrek.Persistence;

/// <summary>
/// Unit of Work implementation.
/// Wraps EF Core's DbContext transaction for multi-repository atomic operations.
/// 
/// Example: VerifyRiddle command:
///   1. team.UnlockCurrentStage() → modifies Team aggregate
///   2. outbox.Add(StageUnlockedEvent) → adds to Outbox
///   3. auditLog.Add(RiddleVerified) → adds AuditLog
///   4. uow.SaveChangesAsync() → ALL three committed atomically
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _db.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
