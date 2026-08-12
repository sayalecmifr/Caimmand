using Caimmand.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TaskEntity = Caimmand.Domain.Entities.Task;

namespace Caimmand.Domain;

public interface ICaimmandDbContext
{
    DbSet<Case> Cases { get; set; }
    DbSet<CaseDefinition> CaseDefinitions { get; set; }
    DbSet<TimelineEvent> TimelineEvents { get; set; }
    DbSet<Participant> Participants { get; set; }
    DbSet<CaseParticipant> CaseParticipants { get; set; }
    DbSet<AuditRecord> AuditRecords { get; set; }
    DbSet<TaskEntity> Tasks { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}