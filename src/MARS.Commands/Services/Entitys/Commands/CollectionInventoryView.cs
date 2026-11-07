using MARS.Shared.Contracts.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

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
