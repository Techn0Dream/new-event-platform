using Microsoft.EntityFrameworkCore;
using TechTrek.Domain.Aggregates;
using TechTrek.Domain.Entities;
using TechTrek.Domain.Interfaces;
using TechTrek.Domain.Primitives;
using TechTrek.Persistence.Configurations;

namespace TechTrek.Persistence;

/// <summary>
/// AppDbContext - single DbContext for the entire TechTrek backend.
/// 
/// Design decisions:
/// - Global query filter for soft-delete: all entities with DeletedAt automatically filtered
/// - All entity configurations defined in separate configuration classes (Fluent API)
/// - No data annotations on domain entities (keeps domain pure)
/// - Uses PostgreSQL-specific features via Npgsql provider
/// - Compiled models ready (call OptimizeContext for production)
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ============================================================
    // DbSets - organized by domain
    // ============================================================

    // Auth / Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // Teams & Membership
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<StageProgress> StageProgresses => Set<StageProgress>();

    // Events & Content
    public DbSet<Domain.Aggregates.Event> Events => Set<Domain.Aggregates.Event>();
    public DbSet<EventStage> EventStages => Set<EventStage>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Riddle> Riddles => Set<Riddle>();

    // Operational
    public DbSet<VolunteerAssignment> VolunteerAssignments => Set<VolunteerAssignment>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global soft-delete filter - entities with DeletedAt are automatically excluded
        // Applied to ALL entities that have a DeletedAt property (AuditableEntity subclasses)
        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
        modelBuilder.Entity<Team>().HasQueryFilter(t => t.DeletedAt == null);
        modelBuilder.Entity<Domain.Aggregates.Event>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Question>().HasQueryFilter(q => q.DeletedAt == null);
        modelBuilder.Entity<Riddle>().HasQueryFilter(r => r.DeletedAt == null);
        modelBuilder.Entity<VolunteerAssignment>().HasQueryFilter(v => v.DeletedAt == null);
        modelBuilder.Entity<Announcement>().HasQueryFilter(a => a.DeletedAt == null);

        // PostgreSQL: use lowercase table names for convention
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName()?.ToSnakeCase());
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()?.ToSnakeCase());
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }
    }
}

/// <summary>Extension to convert PascalCase to snake_case for PostgreSQL conventions.</summary>
internal static class StringExtensions
{
    internal static string? ToSnakeCase(this string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]) && i > 0)
                result.Append('_');
            result.Append(char.ToLower(input[i]));
        }
        return result.ToString();
    }
}
