using Caimmand.Application.Authorization;
using Caimmand.Application.CaseDefinitions.Update;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Task = System.Threading.Tasks.Task;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.CaseDefinitions.Update;

public class UpdateCaseDefinitionHandlerTests
{
    private static UpdateCaseDefinitionHandler BuildHandler(TestDbContext db, IAuthorizationContext? auth = null) =>
        new(db, new UpdateCaseDefinitionValidator(), auth ?? TestAuthorizationContext.AsGerente());

    private static UpdateCaseDefinitionCommand Command(
        Guid id,
        string name = "Recordatorio de Turno",
        string description = "Descripcion actualizada",
        string? category = "Appointments",
        string priority = "Media",
        string color = "#3b82f6",
        string icon = "calendar",
        List<CaseStatus>? allowedStatuses = null) =>
        new(id, name, description, category, priority, color, icon, allowedStatuses);

    [Fact]
    public async Task Update_Existing_Succeeds_AndPersists()
    {
        using var db = TestDbContext.Create();
        var def = new CaseDefinition
        {
            Code = "APPOINTMENT_REMINDER",
            Name = "Viejo",
            Description = "Vieja desc",
            IsActive = true,
            DefaultPriority = "Media",
            DisplayColor = "#3b82f6",
            DisplayIcon = "calendar"
        };
        db.CaseDefinitions.Add(def);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var response = await handler.Handle(Command(def.Id, name: "Nuevo nombre"), default);

        Assert.Equal(def.Id, response.Id);
        Assert.Equal("APPOINTMENT_REMINDER", response.Code);
        Assert.True(response.IsActive);

        var entity = await db.CaseDefinitions.SingleAsync();
        Assert.Equal("Nuevo nombre", entity.Name);
        Assert.Equal("Descripcion actualizada", entity.Description);
    }

    [Fact]
    public async Task Update_NotFound_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(Command(Guid.NewGuid()), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(UpdateCaseDefinitionCommand.Id));
    }

    [Fact]
    public async Task Update_InvalidPriority_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var def = new CaseDefinition { Code = "X", Name = "X", Description = "X", DefaultPriority = "Media", DisplayColor = "#3b82f6", DisplayIcon = "c" };
        db.CaseDefinitions.Add(def);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(Command(def.Id, priority: "Normal"), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(UpdateCaseDefinitionCommand.DefaultPriority));
    }

[Fact]
    public async Task Update_InvalidColorFormat_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var def = new CaseDefinition { Code = "X", Name = "X", Description = "X", DefaultPriority = "Media", DisplayColor = "#3b82f6", DisplayIcon = "c" };
        db.CaseDefinitions.Add(def);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(Command(def.Id, color: "Green"), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(UpdateCaseDefinitionCommand.DisplayColor));
    }

    [Fact]
    public async Task Update_AsOperador_ThrowsUnauthorized()
    {
        using var db = TestDbContext.Create();
        var def = new CaseDefinition { Code = "X", Name = "X", Description = "X", DefaultPriority = "Media", DisplayColor = "#3b82f6", DisplayIcon = "c" };
        db.CaseDefinitions.Add(def);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db, TestAuthorizationContext.AsOperador());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(Command(def.Id), default));
    }

    [Fact]
    public async Task Update_AsSupervisor_ThrowsUnauthorized()
    {
        using var db = TestDbContext.Create();
        var def = new CaseDefinition { Code = "X", Name = "X", Description = "X", DefaultPriority = "Media", DisplayColor = "#3b82f6", DisplayIcon = "c" };
        db.CaseDefinitions.Add(def);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db, TestAuthorizationContext.AsSupervisor());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(Command(def.Id), default));
    }

    [Fact]
    public async Task Update_WithAllowedStatuses_PersistsThem()
    {
        using var db = TestDbContext.Create();
        var def = new CaseDefinition { Code = "X", Name = "X", Description = "X", DefaultPriority = "Media", DisplayColor = "#3b82f6", DisplayIcon = "c" };
        db.CaseDefinitions.Add(def);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var allowed = new List<CaseStatus> { CaseStatus.Creado, CaseStatus.EnCurso };
        await handler.Handle(Command(def.Id, allowedStatuses: allowed), default);

        var entity = await db.CaseDefinitions.SingleAsync();
        Assert.Equal(2, entity.AllowedStatuses.Count);
        Assert.Contains(CaseStatus.EnCurso, entity.AllowedStatuses);
    }

    [Fact]
    public async Task Update_WithoutAllowedStatuses_PreservesExisting()
    {
        using var db = TestDbContext.Create();
        var def = new CaseDefinition
        {
            Code = "X", Name = "X", Description = "X", DefaultPriority = "Media",
            DisplayColor = "#3b82f6", DisplayIcon = "c",
            AllowedStatuses = new List<CaseStatus> { CaseStatus.Creado, CaseStatus.EnCurso, CaseStatus.Finalizado }
        };
        db.CaseDefinitions.Add(def);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        await handler.Handle(Command(def.Id), default);

        var entity = await db.CaseDefinitions.SingleAsync();
        Assert.Equal(3, entity.AllowedStatuses.Count);
    }
}

