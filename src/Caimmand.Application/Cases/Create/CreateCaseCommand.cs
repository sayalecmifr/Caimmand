using System.Text.Json;

namespace Caimmand.Application.Cases.Create;

public sealed record CreateCaseCommand(
    string CaseDefinitionCode,
    string Title,
    string SourceSystem,
    JsonElement Context);