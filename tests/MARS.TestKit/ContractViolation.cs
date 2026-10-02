namespace MARS.TestKit;

/// <summary>
/// Причина, по которой тип или свойство не удалось проверить. Один перечень на
/// проверку контракта, чтобы тест падал с понятным текстом, а не с
/// <c>Assert.Empty</c> по пустому списку строк.
/// </summary>
public enum ContractViolationKind
{
    /// <summary>Публичный тип нельзя создать: нет ни одного конструктора с набором аргументов, который можно подобрать.</summary>
    NotConstructible,

    /// <summary>Конструктор упал. Для типа данных это ошибка: DTO обязан создаваться.</summary>
    ConstructorThrew,

    /// <summary>Сеттер отклонил значение или геттер вернул не то, что положили.</summary>
    PropertyRoundTripFailed,

    /// <summary>Статический конструктор типа упал при первом обращении.</summary>
    StaticInitializerThrew,
}

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
