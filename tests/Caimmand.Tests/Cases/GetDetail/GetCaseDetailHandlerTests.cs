using System.Text.Json;
using Caimmand.Application.Cases.GetDetail;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.Cases.GetDetail;

public class GetCaseDetailHandlerTests
{
    private static readonly JsonElement Context =
        JsonDocument.Parse("""{"patientId":12345,"patientName":"Juan Perez"}""").RootElement.Clone();

    [Fact]
    public async Task GetCaseDetail_ExistingId_ReturnsCaseDetailWithDefinitionName()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition
        {
            Code = "APPOINTMENT_REMINDER",
            Name = "Recordatorio de Turno",
            IsActive = true
        });
        var entity = new Case
        {
            Id = Guid.NewGuid(),
            Title = "Recordatorio del turno de Juan Perez",
            CaseDefinitionCode = "APPOINTMENT_REMINDER",
            Status = CaseStatus.Creado,
            SourceSystem = "HIS",
            Context = JsonDocument.Parse("""{"patientId":12345}"""),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Cases.Add(entity);
        await db.SaveChangesAsync();
        var handler = new GetCaseDetailHandler(db);

        var result = await handler.Handle(new GetCaseDetailQuery(entity.Id), default);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result!.Id);
        Assert.Equal(entity.Title, result.Title);
        Assert.Equal("Recordatorio de Turno", result.CaseDefinitionName);
        Assert.Equal(CaseStatus.Creado, result.Status);
        Assert.Equal("HIS", result.SourceSystem);
        Assert.NotNull(result.Context);
    }

    [Fact]
    public async Task GetCaseDetail_UnknownId_ReturnsNull()
    {
        using var db = TestDbContext.Create();
        var handler = new GetCaseDetailHandler(db);

        var result = await handler.Handle(new GetCaseDetailQuery(Guid.NewGuid()), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCaseDetail_OrphanCaseCode_ReturnsCodeAsNameFallback()
    {
        using var db = TestDbContext.Create();
        var entity = new Case
        {
            Id = Guid.NewGuid(),
            Title = "Caso huerfano",
            CaseDefinitionCode = "ORPHAN_CODE",
            Status = CaseStatus.Creado,
            SourceSystem = "MANUAL",
            Context = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Cases.Add(entity);
        await db.SaveChangesAsync();
        var handler = new GetCaseDetailHandler(db);

        var result = await handler.Handle(new GetCaseDetailQuery(entity.Id), default);

        Assert.NotNull(result);
        Assert.Equal("ORPHAN_CODE", result!.CaseDefinitionName);
    }
}