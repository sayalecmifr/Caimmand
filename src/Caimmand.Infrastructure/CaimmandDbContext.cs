using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Infrastructure;

public class CaimmandDbContext : DbContext, ICaimmandDbContext
{
    public CaimmandDbContext(DbContextOptions<CaimmandDbContext> options) : base(options)
    {
    }

    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseDefinition> CaseDefinitions => Set<CaseDefinition>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();

    DbSet<Case> ICaimmandDbContext.Cases { get => Cases; set { } }
    DbSet<CaseDefinition> ICaimmandDbContext.CaseDefinitions { get => CaseDefinitions; set { } }
    DbSet<TimelineEvent> ICaimmandDbContext.TimelineEvents { get => TimelineEvents; set { } }

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
        });

        modelBuilder.Entity<TimelineEvent>(b =>
        {
            b.ToTable("TimelineEvents");
            b.HasKey(x => x.Id);
            b.Property(x => x.CaseId).IsRequired();
            b.Property(x => x.Sequence).IsRequired();
            b.Property(x => x.Type).IsRequired().HasMaxLength(100);
            b.Property(x => x.Origin).IsRequired().HasMaxLength(100);
            b.Property(x => x.Content).IsRequired();
            b.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
            b.HasIndex(x => new { x.CaseId, x.Sequence }).IsUnique();
            b.HasIndex(x => x.CaseId);
        });
    }
}