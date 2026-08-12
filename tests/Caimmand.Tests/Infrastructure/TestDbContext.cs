using System.Text.Json;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TaskEntity = Caimmand.Domain.Entities.Task;

namespace Caimmand.Tests.Infrastructure;

internal sealed class TestDbContext : DbContext, ICaimmandDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
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

    public static TestDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Case>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Context)
                .HasConversion(
                    v => v.RootElement.GetRawText(),
                    v => JsonDocument.Parse(v));
        });

        modelBuilder.Entity<CaseDefinition>(b =>
        {
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<TimelineEvent>(b =>
        {
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<Participant>(b =>
        {
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<CaseParticipant>(b =>
        {
            b.HasKey(x => new { x.CaseId, x.ParticipantId });
        });

        modelBuilder.Entity<AuditRecord>(b =>
        {
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<TaskEntity>(b =>
        {
            b.HasKey(x => x.Id);
        });
    }
}