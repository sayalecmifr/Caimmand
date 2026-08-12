using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaskEntity = Caimmand.Domain.Entities.Task;

namespace Caimmand.Infrastructure;

public class CaimmandDbContext : DbContext, ICaimmandDbContext
{
    public CaimmandDbContext(DbContextOptions<CaimmandDbContext> options) : base(options)
    {
    }

    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseDefinition> CaseDefinitions => Set<CaseDefinition>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<CaseParticipant> CaseParticipants => Set<CaseParticipant>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    DbSet<Case> ICaimmandDbContext.Cases { get => Cases; set { } }
    DbSet<CaseDefinition> ICaimmandDbContext.CaseDefinitions { get => CaseDefinitions; set { } }
    DbSet<TimelineEvent> ICaimmandDbContext.TimelineEvents { get => TimelineEvents; set { } }
    DbSet<Participant> ICaimmandDbContext.Participants { get => Participants; set { } }
    DbSet<CaseParticipant> ICaimmandDbContext.CaseParticipants { get => CaseParticipants; set { } }
    DbSet<AuditRecord> ICaimmandDbContext.AuditRecords { get => AuditRecords; set { } }
    DbSet<TaskEntity> ICaimmandDbContext.Tasks { get => Tasks; set { } }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Case>(b =>
        {
            b.ToTable("Cases");
            b.HasKey(x => x.Id);
            b.Property(x => x.CaseDefinitionCode).IsRequired().HasMaxLength(100);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            b.Property(x => x.Title).IsRequired().HasMaxLength(300);
            b.Property(x => x.Context).HasColumnType("jsonb").IsRequired();
            b.Property(x => x.SourceSystem).IsRequired().HasMaxLength(100);
            b.Property(x => x.Priority).IsRequired().HasMaxLength(50).HasDefaultValue("Media");
            b.Property(x => x.Sla).HasColumnType("interval");
            b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CaseDefinitionCode);
            b.HasIndex(x => x.SourceSystem);
        });

        modelBuilder.Entity<CaseDefinition>(b =>
        {
            b.ToTable("CaseDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).IsRequired().HasMaxLength(100);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).IsRequired().HasMaxLength(1000);
            b.Property(x => x.Category).HasMaxLength(100);
            b.Property(x => x.DefaultPriority).IsRequired().HasMaxLength(50);
            b.Property(x => x.DisplayColor).IsRequired().HasMaxLength(50);
            b.Property(x => x.DisplayIcon).IsRequired().HasMaxLength(50);
            b.HasIndex(x => x.Code).IsUnique();

            var allowedStatusesConverter = new ValueConverter<List<CaseStatus>, string>(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v) || v == "null"
                    ? new List<CaseStatus>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<CaseStatus>>(v, (System.Text.Json.JsonSerializerOptions?)null)!);

            b.Property(x => x.AllowedStatuses)
                .HasConversion(allowedStatusesConverter)
                .HasColumnType("jsonb")
                .IsRequired();
        });

        modelBuilder.Entity<TimelineEvent>(b =>
        {
            b.ToTable("TimelineEvents");
            b.HasKey(x => x.Id);
            b.Property(x => x.CaseId).IsRequired();
            b.Property(x => x.Sequence).IsRequired();
            b.Property(x => x.Type).IsRequired().HasMaxLength(100);
            b.Property(x => x.Origin).IsRequired().HasMaxLength(100);
            b.Property(x => x.ParticipantId);
            b.Property(x => x.Content).IsRequired();
            b.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
            b.HasIndex(x => new { x.CaseId, x.Sequence }).IsUnique();
            b.HasIndex(x => x.CaseId);
        });

        modelBuilder.Entity<Participant>(b =>
        {
            b.ToTable("Participants");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
            b.Property(x => x.Reference).IsRequired().HasMaxLength(300);
            b.Property(x => x.ExternalId).HasMaxLength(200);
            b.HasIndex(x => x.ExternalId);
        });

        modelBuilder.Entity<CaseParticipant>(b =>
        {
            b.ToTable("CaseParticipants");
            b.HasKey(x => new { x.CaseId, x.ParticipantId });
            b.Property(x => x.Rol).IsRequired().HasMaxLength(100);
            b.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Participant).WithMany().HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.CaseId);
            b.HasIndex(x => x.ParticipantId);
        });

        modelBuilder.Entity<AuditRecord>(b =>
        {
            b.ToTable("AuditRecords");
            b.HasKey(x => x.Id);
            b.Property(x => x.CaseId).IsRequired();
            b.Property(x => x.Operation).HasConversion<string>().HasMaxLength(50).IsRequired();
            b.Property(x => x.Origin).IsRequired().HasMaxLength(200);
            b.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
            b.Property(x => x.ChangeJson).HasColumnType("jsonb").IsRequired();
            b.Property(x => x.ContextRef).HasMaxLength(500);
            b.HasIndex(x => new { x.CaseId, x.OccurredAt });
        });

        modelBuilder.Entity<TaskEntity>(b =>
        {
            b.ToTable("Tasks");
            b.HasKey(x => x.Id);
            b.Property(x => x.CaseId).IsRequired();
            b.Property(x => x.Type).IsRequired().HasMaxLength(100);
            b.Property(x => x.AssigneeId);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            b.Property(x => x.Result);
            b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            b.Property(x => x.StartedAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.DueAt).HasColumnType("timestamp with time zone");
            b.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => new { x.CaseId, x.Status });
            b.HasIndex(x => new { x.AssigneeId, x.Status, x.DueAt });
        });
    }
}