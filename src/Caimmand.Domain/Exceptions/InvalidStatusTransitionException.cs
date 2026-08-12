using Caimmand.Domain.Enums;

namespace Caimmand.Domain.Exceptions;

public sealed class InvalidStatusTransitionException : Exception
{
    public CaseStatus From { get; }
    public CaseStatus To { get; }

    public InvalidStatusTransitionException(CaseStatus from, CaseStatus to)
        : base($"Transicion no valida: {from} -> {to}.")
    {
        From = from;
        To = to;
    }
}