using System.Text.Json;
using Caimmand.Application.Timeline.GetTimeline;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;

namespace Caimmand.Tests.Timeline.GetTimeline;

public class GetTimelineHandlerTests
{
    private static readonly JsonElement Context =
        JsonDocument.Parse("""{"patientId":1}""").RootElement.Clone();

    private static async Task<TestDbContext> SeedWithEventsAsync(Guid caseId, int count)
    {
        var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "X", Name = "X", IsActive = true });
        db.Cases.Add(new Case
        {
            Id = caseId,
            Title = "Caso",
            CaseDefinitionCode = "X",
            Status = CaseStatus.Creado,
            SourceSystem = "HIS",
            Context = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        for (var i = 1; i <= count; i++)
        {
            db.TimelineEvents.Add(new TimelineEvent
            {
                CaseId = caseId,
                Sequence = i,
                Type = $"Evento{i}",
                Origin = "Sistema",
                Content = $"Contenido {i}",
                OccurredAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task GetTimeline_WithEvents_ReturnsDescendingBySequence()
    {
        var caseId = Guid.NewGuid();
        using var db = await SeedWithEventsAsync(caseId, 3);
        var handler = new GetTimelineHandler(db);

        var result = await handler.Handle(new GetTimelineQuery(caseId), default);

        Assert.Equal(3, result.Count);
        Assert.Equal(3, result[0].Sequence);
        Assert.Equal(2, result[1].Sequence);
        Assert.Equal(1, result[2].Sequence);
    }

    [Fact]
    public async Task GetTimeline_NoEvents_ReturnsEmptyList()
    {
        var caseId = Guid.NewGuid();
        using var db = await SeedWithEventsAsync(caseId, 0);
        var handler = new GetTimelineHandler(db);

        var result = await handler.Handle(new GetTimelineQuery(caseId), default);

        Assert.Empty(result);
    }
}