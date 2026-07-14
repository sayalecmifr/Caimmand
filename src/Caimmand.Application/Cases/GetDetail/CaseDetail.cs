using System.Text.Json;
using Caimmand.Domain.Enums;

namespace Caimmand.Application.Cases.GetDetail;

public sealed record CaseDetail(
    Guid Id,
    string Title,
    string CaseDefinitionCode,
    string CaseDefinitionName,
    CaseStatus Status,
    string SourceSystem,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    JsonDocument Context);