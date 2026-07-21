using Caimmand.Application.CaseDefinitions.Create;
using Caimmand.Domain.Entities;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.CaseDefinitions.Create;

public class CreateCaseDefinitionHandlerTests
{
    private static CreateCaseDefinitionHandler BuildHandler(TestDbContext db) =>
        new(db, new CreateCaseDefinitionValidator());

    private static CreateCaseDefinitionCommand Command(
        string code = "APPOINTMENT_REMINDER",
        string name = "Recordatorio de Turno",
        string description = "Recordatorio automatico",
        string? category = "Appointments",
        string priority = "Media",
        string color = "#3b82f6",
        string icon = "calendar") =>
        new(code, name, description, category, priority, color, icon);

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
}