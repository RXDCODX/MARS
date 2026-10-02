using MARS.Shared.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Текст инвентаря коллекции для команд <c>fumoinv</c> и <c>mikuinv</c>.
/// </summary>
/// <remarks>
/// Список обрезается по длине: в чат Twitch нельзя отправить сообщение длиннее
/// 500 символов, а у активного игрока коллекция может быть больше. Обрезка с
/// многоточием честнее, чем отказ показывать что-то вовсе.
/// </remarks>
internal static class CollectionInventoryText
{
    /// <summary>Предел длины списка предметов.</summary>
    public const int MaxListLength = 450;

    public static string Format(string userName, CollectionInventoryView inventory, string unit)
    {
        var result = $"У {userName} собрано {inventory.Collected}/{inventory.Total} {unit}";

        var list = BuildList(inventory);

        if (list.Length > 0)
        {
            result += $". Список: {list}";
        }

        return result;
    }

    private static string BuildList(CollectionInventoryView inventory)
    {
        var result = string.Empty;

        if (inventory.Items.Count > 0)
        {
            var joined = string.Join(
                ", ",
                inventory.Items.Select(item => $"{item.Name} ×{item.Count}")
            );

            result = joined.Length <= MaxListLength ? joined : $"{joined[..MaxListLength]}…";
        }

        return result;
    }
}

/// <summary>
/// Инвентарь в том виде, в каком его отдаёт MARS.WaifuGacha.
/// </summary>
internal sealed record CollectionInventoryView(
    int Collected,
    int Total,
    IReadOnlyList<CollectionItem> Items
)
{
    public static CollectionInventoryView From(CollectionInventory inventory) =>
        new(inventory.Collected, inventory.Total, inventory.Items);
}
