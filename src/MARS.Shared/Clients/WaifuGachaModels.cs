namespace MARS.Shared.Clients;

/// <summary>
/// Позиция инвентаря коллекции: название предмета и сколько раз он выпал.
/// </summary>
/// <remarks>
/// Тип лежит в MARS.Shared: его сериализует контроллер <c>MARS.WaifuGacha</c>
/// и десериализует клиент команды <c>MARS.Commands</c>.
/// </remarks>
public sealed record CollectionItem(string Name, int Count);

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

/// <summary>
/// Ссылка на произведение на Shikimori в том виде, в каком его показывает
/// команда: id, названия, год и готовая ссылка.
/// </summary>
public sealed record ShikimoriTitleRef(
    long Id,
    string? Name,
    string? RussianName,
    int? Year,
    string Url
);
