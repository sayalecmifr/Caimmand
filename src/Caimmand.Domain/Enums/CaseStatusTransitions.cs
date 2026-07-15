namespace Caimmand.Domain.Enums;

public static class CaseStatusTransitions
{
    private static readonly Dictionary<CaseStatus, IReadOnlySet<CaseStatus>> Valid = new()
    {
        [CaseStatus.Creado] = new HashSet<CaseStatus> { CaseStatus.EnCurso },
        [CaseStatus.EnCurso] = new HashSet<CaseStatus> { CaseStatus.Suspendido, CaseStatus.Finalizado, CaseStatus.Cancelado },
        [CaseStatus.Suspendido] = new HashSet<CaseStatus> { CaseStatus.EnCurso, CaseStatus.Cancelado },
        [CaseStatus.Finalizado] = new HashSet<CaseStatus>(),
        [CaseStatus.Cancelado] = new HashSet<CaseStatus>(),
    };

    public static bool IsValid(CaseStatus from, CaseStatus to) =>
        Valid.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyCollection<CaseStatus> GetValidTargets(CaseStatus from) =>
        Valid.TryGetValue(from, out var targets) ? targets : Array.Empty<CaseStatus>();
}