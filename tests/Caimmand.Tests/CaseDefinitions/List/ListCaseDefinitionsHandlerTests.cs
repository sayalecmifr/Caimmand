using Caimmand.Application.CaseDefinitions.List;
using Caimmand.Domain.Entities;
using Caimmand.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.CaseDefinitions.List;

public class ListCaseDefinitionsHandlerTests
{
    private static ListCaseDefinitionsHandler BuildHandler(TestDbContext db) => new(db);

    private static CaseDefinition Def(string code, string name, bool active = true) => new()
    {
        Code = code,
        Name = name,
        IsActive = active,
        DefaultPriority = "Media",
        DisplayColor = "#6c757d",
        DisplayIcon = "folder"
    };

    [Fact]
    public async Task List_ReturnsAllDefinitions_OrderedByName()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(Def("ZZZ", "Zeta"));
        db.CaseDefinitions.Add(Def("AAA", "Alfa"));
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var result = await handler.Handle(new ListCaseDefinitionsQuery(), default);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alfa", result[0].Name);
        Assert.Equal("Zeta", result[1].Name);
    }

    [Fact]
    public async Task List_ReturnsActiveAndInactive()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(Def("ACTIVE", "Activa", active: true));
        db.CaseDefinitions.Add(Def("INACTIVE", "Inactiva", active: false));
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var result = await handler.Handle(new ListCaseDefinitionsQuery(), default);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.IsActive);
        Assert.Contains(result, d => !d.IsActive);
    }

    [Fact]
    public async Task List_WhenEmpty_ReturnsEmptyList()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        var result = await handler.Handle(new ListCaseDefinitionsQuery(), default);

        Assert.Empty(result);
    }
}