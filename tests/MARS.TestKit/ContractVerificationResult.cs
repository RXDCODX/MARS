namespace MARS.TestKit;

/// <summary>
/// Итог проверки контракта: что проверили и что не сошлось. Тест должен падать
/// на <see cref="Violations"/>, а объём проверки виден отдельными числами — иначе
/// проверка, случайно не нашедшая ни одного типа, выглядит как успешная.
/// </summary>
public sealed class ContractVerificationResult
{
    private readonly List<ContractViolation> _violations = [];
    private readonly List<string> _skipped = [];

    public IReadOnlyList<ContractViolation> Violations => _violations;

    /// <summary>
    /// Типы, которые пропущены осознанно: их нельзя создать из подставляемых
    /// значений (нужны настоящие зависимости) либо они перечислены в
    /// <see cref="ContractOptions.IgnoredTypes"/>. Пропуск не считается нарушением,
    /// но и не считается проверкой.
    /// </summary>
    public IReadOnlyList<string> SkippedTypes => _skipped;

    public int TypesInspected { get; private set; }

    public int TypesConstructed { get; private set; }

    public int ConstructorsInvoked { get; private set; }

    public int PropertiesChecked { get; private set; }

    public int StaticInitializersRun { get; private set; }

    internal void AddViolation(ContractViolationKind kind, Type type, string? member, string detail)
    {
        _violations.Add(new ContractViolation(kind, type.FullName ?? type.Name, member, detail));
    }

    internal void SkipType(Type type, string reason) =>
        _skipped.Add($"{type.FullName ?? type.Name}: {reason}");

    internal void CountType() => TypesInspected++;

    internal void CountConstructed() => TypesConstructed++;

    internal void CountConstructor() => ConstructorsInvoked++;

    internal void CountProperty() => PropertiesChecked++;

    internal void CountStaticInitializer() => StaticInitializersRun++;
}
