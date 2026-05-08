using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechTrek.Domain.Aggregates;
using TechTrek.Domain.Entities;

namespace TechTrek.Persistence.Configurations;

// ============================================================
// USER CONFIGURATION
// ============================================================

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // UUIDs generated in domain

        builder.Property(u => u.Username)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        // Email stored as encrypted ciphertext - mapped from EncryptedField VO
        builder.Property(u => u.Email)
            .HasConversion(
                e => e.CipherText,
                s => Domain.ValueObjects.EncryptedField.FromCipherText(s))
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>() // Store as string for readability
            .HasMaxLength(20);

        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.College).HasMaxLength(200);
        builder.Property(u => u.PhoneEncrypted).HasMaxLength(512);

        // Indexes: fast lookups, unique constraints
        builder.HasIndex(u => u.Username).IsUnique();

        // Navigation: owned tokens
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.TeamMemberships)
            .WithOne(tm => tm.User)
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(u => u.DomainEvents);
    }
}

// ============================================================
// REFRESH TOKEN CONFIGURATION
// ============================================================

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).ValueGeneratedNever();
        builder.Property(rt => rt.TokenHash).HasMaxLength(512).IsRequired();
        builder.Property(rt => rt.FamilyId).HasMaxLength(100).IsRequired();
        builder.Property(rt => rt.DeviceFingerprint).HasMaxLength(256);
        builder.Property(rt => rt.IpAddress).HasMaxLength(50);
        builder.Property(rt => rt.UserAgent).HasMaxLength(500);
        builder.Property(rt => rt.RevokedReason).HasMaxLength(200);

        // Index for fast token lookup by hash
        builder.HasIndex(rt => rt.TokenHash).IsUnique();
        // Index for family-based revocation
        builder.HasIndex(rt => rt.FamilyId);
        // Index for user-based queries
        builder.HasIndex(rt => rt.UserId);
    }
}

// ============================================================
// TEAM CONFIGURATION
// ============================================================

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();

        // Score: stored as int, converted from/to Score value object
        builder.Property(t => t.Score)
            .HasConversion(
                s => s.Value,
                v => Domain.ValueObjects.Score.Of(v));

        // Owned collections (aggregate children)
        builder.HasMany(t => t.Members)
            .WithOne(m => m.Team)
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.StageProgresses)
            .WithOne(sp => sp.Team)
            .HasForeignKey(sp => sp.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for leaderboard query: all teams in event ordered by score
        builder.HasIndex(t => new { t.EventId, t.Score });

        builder.Ignore(t => t.DomainEvents);
    }
}

public sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);

        // Composite unique: one user per team
        builder.HasIndex(m => new { m.TeamId, m.UserId }).IsUnique();
    }
}

public sealed class StageProgressConfiguration : IEntityTypeConfiguration<StageProgress>
{
    public void Configure(EntityTypeBuilder<StageProgress> builder)
    {
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Id).ValueGeneratedNever();
        builder.Property(sp => sp.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(sp => sp.SubmittedCode).HasColumnType("text"); // Unlimited length

        // Key game state query: team's progress at a specific stage
        builder.HasIndex(sp => new { sp.TeamId, sp.StageIndex }).IsUnique();
    }
}

// ============================================================
// EVENT CONFIGURATION
// ============================================================

public sealed class EventConfiguration : IEntityTypeConfiguration<Domain.Aggregates.Event>
{
    public void Configure(EntityTypeBuilder<Domain.Aggregates.Event> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);

        builder.Ignore(e => e.IsTimerRunning);
        builder.Ignore(e => e.RemainingSeconds);
        builder.Ignore(e => e.DomainEvents);

        builder.HasMany(e => e.Stages)
            .WithOne()
            .HasForeignKey(es => es.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================
// SUPPORTING ENTITY CONFIGURATIONS
// ============================================================

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.EventType).HasMaxLength(200).IsRequired();
        builder.Property(o => o.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(o => o.Error).HasMaxLength(2000);

        builder.Ignore(o => o.IsProcessed);

        // Index for polling unprocessed messages efficiently
        builder.HasIndex(o => o.ProcessedAt);
        builder.HasIndex(o => new { o.ProcessedAt, o.RetryCount });
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.PayloadJson).HasColumnType("jsonb");
        builder.Property(a => a.FailureReason).HasMaxLength(500);

        // Admin log feed queries
        builder.HasIndex(a => new { a.ActorId, a.CreatedAt });
        builder.HasIndex(a => a.CreatedAt);
    }
}

public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.Key).HasMaxLength(100).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(500);

        builder.HasIndex(f => new { f.Key, f.EventId }).IsUnique();
    }
}

public sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Title).HasMaxLength(300).IsRequired();
        builder.Property(a => a.Body).HasColumnType("text").IsRequired();

        builder.HasIndex(a => new { a.EventId, a.IsPublished });
        builder.HasIndex(a => a.ScheduledAt);
    }
}

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedNever();
        builder.Property(q => q.Title).HasMaxLength(300).IsRequired();
        builder.Property(q => q.Description).HasColumnType("text").IsRequired();
        builder.Property(q => q.InputFormat).HasColumnType("text").IsRequired();
        builder.Property(q => q.OutputFormat).HasColumnType("text").IsRequired();
        builder.Property(q => q.SampleInput).HasColumnType("text");
        builder.Property(q => q.SampleOutput).HasColumnType("text");
        builder.Property(q => q.Constraints).HasColumnType("text");

        
    }
}

public sealed class RiddleConfiguration : IEntityTypeConfiguration<Riddle>
{
    public void Configure(EntityTypeBuilder<Riddle> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.QuestionText).HasColumnType("text").IsRequired();
        builder.Property(r => r.AnswerEncrypted).HasMaxLength(1024).IsRequired(); // AES-256 ciphertext
        builder.Property(r => r.Location).HasMaxLength(200).IsRequired();

        
    }
}
