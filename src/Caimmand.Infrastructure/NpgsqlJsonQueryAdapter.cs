using System.Text.Json;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Infrastructure;

public sealed class NpgsqlJsonQueryAdapter : IJsonQueryAdapter
{
    public IQueryable<Case> WhereExternalId(IQueryable<Case> source, string externalId)
    {
        var filterJson = JsonSerializer.SerializeToElement(new { externalId });
        return source.Where(c => EF.Functions.JsonContains(c.Context, filterJson));
    }
}