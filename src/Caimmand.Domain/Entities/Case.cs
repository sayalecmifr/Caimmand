using Caimmand.Domain.Enums;

namespace Caimmand.Domain.Entities;

public class Case
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CaseDefinitionCode { get; set; } = string.Empty;
    public CaseStatus Status { get; set; } = CaseStatus.Creado;
    public string Title { get; set; } = string.Empty;
    public string Context { get; set; } = "{}";
    public string SourceSystem { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}