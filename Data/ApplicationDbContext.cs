using Microsoft.EntityFrameworkCore;
using RayanTask.Models;

namespace RayanTask.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ActivityRecord> ActivityRecords { get; set; } = null!;
    public DbSet<ActivityType> ActivityTypes { get; set; } = null!;
    public DbSet<Result> Results { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<SoftwareIssue> SoftwareIssues { get; set; } = null!;
    public DbSet<SoftwareName> SoftwareNames { get; set; } = null!;
    public DbSet<IssueType> IssueTypes { get; set; } = null!;
    public DbSet<Status> Statuses { get; set; } = null!;
    public DbSet<IssueComment> IssueComments { get; set; } = null!;
    public DbSet<IssueAttachment> IssueAttachments { get; set; } = null!;
    public DbSet<CommentAttachment> CommentAttachments { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
    public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- User ----
        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.FirstName, u.LastName })
            .IsUnique();

        // ---- Lookup tables: Value unique ----
        modelBuilder.Entity<ActivityType>()
            .HasIndex(at => at.Value)
            .IsUnique();

        modelBuilder.Entity<Result>()
            .HasIndex(r => r.Value)
            .IsUnique();

        modelBuilder.Entity<SoftwareName>()
            .HasIndex(sn => sn.Value)
            .IsUnique();

        modelBuilder.Entity<IssueType>()
            .HasIndex(it => it.Value)
            .IsUnique();

        modelBuilder.Entity<Status>()
            .HasIndex(s => s.Value)
            .IsUnique();

        // ---- ActivityRecord ----
        modelBuilder.Entity<ActivityRecord>()
            .HasOne(ar => ar.User)
            .WithMany()
            .HasForeignKey(ar => ar.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ActivityRecord>()
            .HasOne(ar => ar.Result)
            .WithMany()
            .HasForeignKey(ar => ar.ResultId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-many ActivityRecord <-> ActivityType via explicit join entity
        modelBuilder.Entity<ActivityRecord>()
            .HasMany(ar => ar.ActivityTypes)
            .WithMany()
            .UsingEntity<ActivityRecordActivityType>(
                j => j.HasOne(x => x.ActivityType)
                    .WithMany()
                    .HasForeignKey(x => x.ActivityTypeId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne(x => x.ActivityRecord)
                    .WithMany()
                    .HasForeignKey(x => x.ActivityRecordId)
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey(x => new { x.ActivityRecordId, x.ActivityTypeId });
                    j.ToTable("ActivityRecordActivityType");
                });

        // ---- SoftwareIssue ----
        modelBuilder.Entity<SoftwareIssue>()
            .HasOne(si => si.SoftwareNameEntity)
            .WithMany()
            .HasForeignKey(si => si.SoftwareNameId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SoftwareIssue>()
            .HasOne(si => si.IssueTypeEntity)
            .WithMany()
            .HasForeignKey(si => si.IssueTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SoftwareIssue>()
            .HasOne(si => si.ReporterEntity)
            .WithMany()
            .HasForeignKey(si => si.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SoftwareIssue>()
            .HasOne(si => si.AssignedToEntity)
            .WithMany()
            .HasForeignKey(si => si.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SoftwareIssue>()
            .HasOne(si => si.StatusEntity)
            .WithMany()
            .HasForeignKey(si => si.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- IssueAttachment / CommentAttachment ----
        modelBuilder.Entity<IssueAttachment>()
            .HasOne(ia => ia.Issue)
            .WithMany()
            .HasForeignKey(ia => ia.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommentAttachment>()
            .HasOne(ca => ca.Comment)
            .WithMany()
            .HasForeignKey(ca => ca.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- PasswordResetToken / PasswordResetRequest ----
        modelBuilder.Entity<PasswordResetToken>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(t => t.Token)
            .IsUnique();

        modelBuilder.Entity<PasswordResetRequest>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PasswordResetRequest>()
            .HasIndex(t => t.RequestToken)
            .IsUnique();
    }
}
