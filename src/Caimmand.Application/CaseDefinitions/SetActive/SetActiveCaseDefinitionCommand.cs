namespace Caimmand.Application.CaseDefinitions.SetActive;

public sealed record SetActiveCaseDefinitionCommand(Guid Id, bool IsActive);