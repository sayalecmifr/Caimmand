namespace Caimmand.Domain.Entities;

public class CaseDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public TimeSpan? DefaultSla { get; set; }
    public string DefaultPriority { get; set; } = "Media";
    public string DisplayColor { get; set; } = "#6c757d";
    public string DisplayIcon { get; set; } = "folder";
}