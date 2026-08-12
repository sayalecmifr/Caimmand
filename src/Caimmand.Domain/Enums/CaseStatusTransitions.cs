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

    public static bool IsValid(CaseStatus from, CaseStatus to, IReadOnlyCollection<CaseStatus>? allowed)
    {
        if (!IsValid(from, to))
        {
            return false;
        }

        if (allowed is null || allowed.Count == 0)
        {
            return true;
        }

        return allowed.Contains(to);
    }

    public static IReadOnlyCollection<CaseStatus> GetValidTargets(CaseStatus from) =>
        Valid.TryGetValue(from, out var targets) ? targets : Array.Empty<CaseStatus>();

    public static IReadOnlyCollection<CaseStatus> GetValidTargets(CaseStatus from, IReadOnlyCollection<CaseStatus>? allowed)
    {
        var global = GetValidTargets(from);
        if (allowed is null || allowed.Count == 0)
        {
            return global;
        }

        return global.Intersect(allowed).ToArray();
    }
}