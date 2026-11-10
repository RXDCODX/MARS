namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Инвентарь коллекции пользователя.
/// </summary>
/// <remarks>
/// <c>Collected</c> — число разных предметов, <c>Total</c> — размер всей
/// коллекции. В монолите эти два числа возвращались кортежем вперемешку со
/// списком предметов, и перепутать их на месте было легко.
/// </remarks>
public sealed record CollectionInventory(
    int Collected,
    int Total,
    IReadOnlyList<CollectionItem> Items
);
