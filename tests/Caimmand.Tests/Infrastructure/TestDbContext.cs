using System.Text.Json;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.Infrastructure;

internal sealed class TestDbContext : DbContext, ICaimmandDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseDefinition> CaseDefinitions => Set<CaseDefinition>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();

    DbSet<Case> ICaimmandDbContext.Cases { get => Cases; set { } }
    DbSet<CaseDefinition> ICaimmandDbContext.CaseDefinitions { get => CaseDefinitions; set { } }
    DbSet<TimelineEvent> ICaimmandDbContext.TimelineEvents { get => TimelineEvents; set { } }

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
            b.Property(x => x.Context)
                .HasConversion(
                    v => v.RootElement.GetRawText(),
                    v => JsonDocument.Parse(v));
        });
    }
}