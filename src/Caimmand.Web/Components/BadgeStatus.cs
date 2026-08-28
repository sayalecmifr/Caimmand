using Caimmand.Domain.Enums;

namespace Caimmand.Web.Components;

/// <summary>
/// Helpers centralizados de labels y badges para estados de Casos y Tareas.
/// Reemplaza los helpers duplicados en <c>Cases.razor</c>, <c>Tasks.razor</c>,
/// <c>CaseView.razor</c> y <c>CaseDefinitions.razor</c>.
/// </summary>
public static class BadgeStatus
{
    public static string CaseLabel(CaseStatus status) => status switch
    {
        CaseStatus.Creado => "Creado",
        CaseStatus.EnCurso => "En curso",
        CaseStatus.Suspendido => "Suspendido",
        CaseStatus.Finalizado => "Finalizado",
        CaseStatus.Cancelado => "Cancelado",
        _ => status.ToString()
    };

    public static string CaseBadge(CaseStatus status) => BadgeColor(status switch
    {
        CaseStatus.Creado => "primary",
        CaseStatus.EnCurso => "info",
        CaseStatus.Suspendido => "warning",
        CaseStatus.Finalizado => "success",
        CaseStatus.Cancelado => "danger",
        _ => "secondary"
    });

    public static string TaskLabel(string status) => status switch
    {
        "Pendiente" => "Pendiente",
        "EnProgreso" => "En progreso",
        "Completada" => "Completada",
        "Cancelada" => "Cancelada",
        _ => status
    };

    public static string TaskBadge(string status) => BadgeColor(status switch
    {
        "Pendiente" => "secondary",
        "EnProgreso" => "info",
        "Completada" => "success",
        "Cancelada" => "danger",
        _ => "secondary"
    });

    private static string BadgeColor(string color) =>
        $"badge rounded-pill border bg-{color}-subtle text-{color}-emphasis border-{color}-subtle";
}
