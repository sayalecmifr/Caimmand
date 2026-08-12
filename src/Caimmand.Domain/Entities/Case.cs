using System.Text.Json;
using Caimmand.Domain.Enums;

namespace Caimmand.Domain.Entities;

public class Case
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CaseDefinitionCode { get; set; } = string.Empty;
    public CaseStatus Status { get; set; } = CaseStatus.Creado;
    public string Title { get; set; } = string.Empty;
    public JsonDocument Context { get; set; } = JsonDocument.Parse("{}");
    public string SourceSystem { get; set; } = string.Empty;
    public string Priority { get; set; } = "Media";
    public TimeSpan? Sla { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}