using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Application.Cases.Create;
using Caimmand.Domain.Entities;
using Task = System.Threading.Tasks.Task;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.Cases.Create;

public class CreateCaseHandlerTests
{
    private static readonly JsonElement ValidContext =
        JsonDocument.Parse("""{"patientId":12345,"patientName":"Juan Perez"}""").RootElement.Clone();

    private static CreateCaseHandler BuildHandler(TestDbContext db) =>
        new(db, new CreateCaseValidator(), new AuditRecorder(db));

    private static CreateCaseCommand Command(
        string code = "APPOINTMENT_REMINDER",
        string title = "Recordatorio del turno de Juan Perez",
        string source = "HIS",
        JsonElement? context = null,
        string? priority = null,
        TimeSpan? sla = null) =>
        new(code, title, source, context ?? ValidContext, priority, sla);

    private static CaseDefinition ActiveDefinition() => new()
    {
        Code = "APPOINTMENT_REMINDER",
        Name = "Recordatorio de Turno",
        IsActive = true,
        DefaultPriority = "Media",
        DefaultSla = TimeSpan.FromHours(48)
    };

    private static CaseDefinition InactiveDefinition() => new()
    {
        Code = "INACTIVE_DEF",
        Name = "Inactiva",
        IsActive = false
    };

    [Fact]
    public async Task CreateCase_Succeeds_ReturnsResponseWithCreado()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(ActiveDefinition());
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var response = await handler.Handle(Command(), default);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(CaseStatus.Creado.ToString(), response.Status);

        var entity = await db.Cases.SingleAsync();
        Assert.Equal(response.Id, entity.Id);
        Assert.Equal(CaseStatus.Creado, entity.Status);

        var timelineEvent = await db.TimelineEvents.SingleAsync();
        Assert.Equal("Creacion", timelineEvent.Type);
        Assert.Equal("HIS", timelineEvent.Origin);
        Assert.Equal(1, timelineEvent.Sequence);
    }

    [Fact]
    public async Task CreateCase_GeneratesAuditRecord()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(ActiveDefinition());
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        await handler.Handle(Command(), default);

        var audit = await db.AuditRecords.SingleAsync();
        Assert.Equal(AuditOperation.CaseCreation, audit.Operation);
        Assert.Equal("HIS", audit.Origin);
        Assert.NotEmpty(audit.ChangeJson);
    }

    [Fact]
    public async Task CreateCase_InheritsPriorityAndSla_FromDefinition_WhenNotProvided()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(ActiveDefinition());
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        await handler.Handle(Command(), default);

        var entity = await db.Cases.SingleAsync();
        Assert.Equal("Media", entity.Priority);
        Assert.Equal(TimeSpan.FromHours(48), entity.Sla);
    }

    [Fact]
    public async Task CreateCase_UsesProvidedPriorityAndSla_WhenProvided()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(ActiveDefinition());
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        await handler.Handle(Command(priority: "Alta", sla: TimeSpan.FromHours(12)), default);

        var entity = await db.Cases.SingleAsync();
        Assert.Equal("Alta", entity.Priority);
        Assert.Equal(TimeSpan.FromHours(12), entity.Sla);
    }

    [Fact]
    public async Task CreateCase_UnknownDefinition_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(Command(code: "UNKNOWN"), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateCaseCommand.CaseDefinitionCode));
    }

    [Fact]
    public async Task CreateCase_InactiveDefinition_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(InactiveDefinition());
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(Command(code: "INACTIVE_DEF"), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateCaseCommand.CaseDefinitionCode));
    }

    [Fact]
    public async Task CreateCase_EmptyTitle_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(ActiveDefinition());
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(Command(title: ""), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateCaseCommand.Title));
    }

    [Fact]
    public async Task CreateCase_EmptySourceSystem_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(ActiveDefinition());
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(Command(source: ""), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateCaseCommand.SourceSystem));
    }
}
