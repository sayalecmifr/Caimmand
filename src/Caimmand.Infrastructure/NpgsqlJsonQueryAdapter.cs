using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Infrastructure;

public sealed class NpgsqlJsonQueryAdapter : IJsonQueryAdapter
{
    public IQueryable<Case> WhereExternalId(IQueryable<Case> source, string externalId)
    {
        return source.Where(c => c.Context.RootElement.GetProperty("externalId").GetString() == externalId);
    }
}