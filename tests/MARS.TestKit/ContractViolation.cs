namespace MARS.TestKit;

/// <summary>
/// Одно нарушение контракта публичного типа. <see cref="Member"/> пуст у нарушения
/// типа целиком и содержит имя свойства — у нарушения свойства.
/// </summary>
public sealed record ContractViolation(
    ContractViolationKind Kind,
    string TypeName,
    string? Member,
    string Detail
)
{
    public override string ToString() =>
        Member is null
            ? $"{Kind}: {TypeName} — {Detail}"
            : $"{Kind}: {TypeName}.{Member} — {Detail}";
}
