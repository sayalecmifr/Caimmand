using Caimmand.Application.Authorization;
using Caimmand.Application.CaseDefinitions.Create;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Tests.CaseDefinitions.Create;

public class CreateCaseDefinitionHandlerTests
{
    private static CreateCaseDefinitionHandler BuildHandler(TestDbContext db, IAuthorizationContext? auth = null) =>
        new(db, new CreateCaseDefinitionValidator(), auth ?? TestAuthorizationContext.AsGerente());

    private static CreateCaseDefinitionCommand Command(
        string code = "APPOINTMENT_REMINDER",
        string name = "Recordatorio de Turno",
        string description = "Recordatorio automatico",
        string? category = "Appointments",
        string priority = "Media",
        string color = "#3b82f6",
        string icon = "calendar",
        List<CaseStatus>? allowedStatuses = null) =>
        new(code, name, description, category, priority, color, icon, allowedStatuses);

    [Fact]
    public async Task Create_Succeeds_PersistsActiveDefinition()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        var response = await handler.Handle(Command(), default);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("APPOINTMENT_REMINDER", response.Code);

        var entity = await db.CaseDefinitions.SingleAsync();
        Assert.Equal("APPOINTMENT_REMINDER", entity.Code);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public async Task Create_DuplicateCode_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "APPOINTMENT_REMINDER", Name = "Previo" });
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(Command(), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateCaseDefinitionCommand.Code));
    }

    [Fact]
    public async Task Create_InvalidPriority_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(Command(priority: "Normal"), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateCaseDefinitionCommand.DefaultPriority));
    }

    [Fact]
    public async Task Create_InvalidColorFormat_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(Command(color: "Blue"), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateCaseDefinitionCommand.DisplayColor));
    }

    [Fact]
    public async Task Create_EmptyCode_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(Command(code: ""), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateCaseDefinitionCommand.Code));
    }

[Fact]
    public async Task Create_EmptyName_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(Command(name: ""), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateCaseDefinitionCommand.Name));
    }

    [Fact]
    public async Task Create_AsOperador_ThrowsUnauthorized()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db, TestAuthorizationContext.AsOperador());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => handler.Handle(Command(), default));
    }

    [Fact]
    public async Task Create_AsSupervisor_ThrowsUnauthorized()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db, TestAuthorizationContext.AsSupervisor());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => handler.Handle(Command(), default));
    }

    [Fact]
    public async Task Create_AsGerente_Succeeds()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db, TestAuthorizationContext.AsGerente());

        var response = await handler.Handle(Command(), default);

        Assert.NotEqual(Guid.Empty, response.Id);
    }

    [Fact]
    public async Task Create_WithAllowedStatuses_PersistsThem()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        var allowed = new List<CaseStatus> { CaseStatus.Creado, CaseStatus.EnCurso, CaseStatus.Finalizado };
        await handler.Handle(Command(allowedStatuses: allowed), default);

        var entity = await db.CaseDefinitions.SingleAsync();
        Assert.Equal(3, entity.AllowedStatuses.Count);
        Assert.Contains(CaseStatus.Finalizado, entity.AllowedStatuses);
    }

    [Fact]
    public async Task Create_WithoutAllowedStatuses_PersistsEmptyList()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        await handler.Handle(Command(), default);

        var entity = await db.CaseDefinitions.SingleAsync();
        Assert.Empty(entity.AllowedStatuses);
    }
}

