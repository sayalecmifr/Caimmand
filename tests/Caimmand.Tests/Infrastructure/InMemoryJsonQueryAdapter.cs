using System.Text.Json;
using Caimmand.Domain;
using Caimmand.Domain.Entities;

namespace Caimmand.Tests.Infrastructure;

internal static class JsonHelpers
{
    public static bool ExternalIdMatches(JsonDocument context, string externalId)
    {
        return context.RootElement.TryGetProperty("externalId", out var ext)
            && ext.ValueKind == JsonValueKind.String
            && ext.GetString() == externalId;
    }
}

internal sealed class InMemoryJsonQueryAdapter : IJsonQueryAdapter
{
    public IQueryable<Case> WhereExternalId(IQueryable<Case> source, string externalId)
    {
        return source.Where(c => JsonHelpers.ExternalIdMatches(c.Context, externalId));
    }
}