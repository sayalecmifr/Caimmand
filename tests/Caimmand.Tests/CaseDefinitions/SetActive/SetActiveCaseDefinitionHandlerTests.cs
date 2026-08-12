using Caimmand.Application.Authorization;
using Caimmand.Application.CaseDefinitions.SetActive;
using Caimmand.Domain.Entities;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Tests.CaseDefinitions.SetActive;

public class SetActiveCaseDefinitionHandlerTests
{
    private static SetActiveCaseDefinitionHandler BuildHandler(TestDbContext db, IAuthorizationContext? auth = null) =>
        new(db, auth ?? TestAuthorizationContext.AsGerente());

    [Fact]
    public async Task SetActive_True_PersistsIsActiveTrue()
    {
        using var db = TestDbContext.Create();
        var def = new CaseDefinition { Code = "X", Name = "X", Description = "X", IsActive = false, DefaultPriority = "Media", DisplayColor = "#3b82f6", DisplayIcon = "c" };
        db.CaseDefinitions.Add(def);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var response = await handler.Handle(new SetActiveCaseDefinitionCommand(def.Id, true), default);

        Assert.True(response.IsActive);
        var entity = await db.CaseDefinitions.SingleAsync();
        Assert.True(entity.IsActive);
    }

    [Fact]
    public async Task SetActive_False_PersistsIsActiveFalse()
    {
        using var db = TestDbContext.Create();
        var def = new CaseDefinition { Code = "X", Name = "X", Description = "X", IsActive = true, DefaultPriority = "Media", DisplayColor = "#3b82f6", DisplayIcon = "c" };
        db.CaseDefinitions.Add(def);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var response = await handler.Handle(new SetActiveCaseDefinitionCommand(def.Id, false), default);

        Assert.False(response.IsActive);
        var entity = await db.CaseDefinitions.SingleAsync();
        Assert.False(entity.IsActive);
    }

[Fact]
    public async Task SetActive_NotFound_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new SetActiveCaseDefinitionCommand(Guid.NewGuid(), true), default));
    }

    [Fact]
    public async Task SetActive_AsOperador_ThrowsUnauthorized()
    {
        using var db = TestDbContext.Create();
        var def = new CaseDefinition { Code = "X", Name = "X", Description = "X", IsActive = true, DefaultPriority = "Media", DisplayColor = "#3b82f6", DisplayIcon = "c" };
        db.CaseDefinitions.Add(def);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db, TestAuthorizationContext.AsOperador());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new SetActiveCaseDefinitionCommand(def.Id, false), default));
    }

    [Fact]
    public async Task SetActive_AsSupervisor_ThrowsUnauthorized()
    {
        using var db = TestDbContext.Create();
        var def = new CaseDefinition { Code = "X", Name = "X", Description = "X", IsActive = true, DefaultPriority = "Media", DisplayColor = "#3b82f6", DisplayIcon = "c" };
        db.CaseDefinitions.Add(def);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db, TestAuthorizationContext.AsSupervisor());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new SetActiveCaseDefinitionCommand(def.Id, false), default));
    }
}

