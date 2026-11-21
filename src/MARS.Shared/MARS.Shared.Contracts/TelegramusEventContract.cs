using System.Reflection;

namespace MARS.Shared.Contracts;

/// <summary>
/// Ветки <see cref="TelegramusEvent"/> — частный случай
/// <see cref="OneofEventContract"/>. Точка входа для диспетчера оверлея и
/// генератора контракта хаба.
/// </summary>
public static class TelegramusEventContract
{
    public static IReadOnlyList<PropertyInfo> Branches =>
        OneofEventContract.Branches(typeof(TelegramusEvent));

    public static (string Name, object? Payload)? GetActiveBranch(TelegramusEvent notification) =>
        OneofEventContract.GetActiveBranch(notification);
}
