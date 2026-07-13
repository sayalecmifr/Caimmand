using Caimmand.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Domain;

public interface ICaimmandDbContext
{
    DbSet<Case> Cases { get; set; }
    DbSet<CaseDefinition> CaseDefinitions { get; set; }
    DbSet<TimelineEvent> TimelineEvents { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}