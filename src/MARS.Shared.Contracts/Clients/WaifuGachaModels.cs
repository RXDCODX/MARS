namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Позиция инвентаря коллекции: название предмета и сколько раз он выпал.
/// </summary>
/// <remarks>
/// Тип лежит в MARS.Shared: его сериализует контроллер <c>MARS.WaifuGacha</c>
/// и десериализует клиент команды <c>MARS.Commands</c>.
/// </remarks>
public sealed record CollectionItem(string Name, int Count);
