using Caimmand.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Domain;

public interface IJsonQueryAdapter
{
    IQueryable<Case> WhereExternalId(IQueryable<Case> source, string externalId);
}