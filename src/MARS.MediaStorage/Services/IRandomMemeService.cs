using MARS.MediaStorage.Entities;

namespace MARS.MediaStorage.Services;

public interface IRandomMemeService
{
    Task<IEnumerable<MemeType>> GetAllMemeTypesAsync(CancellationToken cancellationToken = default);
    Task<MemeType?> GetMemeTypeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<MemeType> CreateMemeTypeAsync(
        MemeType memeType,
        CancellationToken cancellationToken = default
    );
    Task<MemeType> UpdateMemeTypeAsync(
        MemeType memeType,
        CancellationToken cancellationToken = default
    );
    Task<bool> DeleteMemeTypeAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<MemeOrder>> GetAllMemeOrdersAsync(
        CancellationToken cancellationToken = default
    );
    Task<MemeOrder?> GetMemeOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<MemeOrder>> GetMemeOrdersByTypeAsync(
        int typeId,
        CancellationToken cancellationToken = default
    );
    Task<MemeOrder> CreateMemeOrderAsync(
        MemeOrder memeOrder,
        CancellationToken cancellationToken = default
    );
    Task<MemeOrder> UpdateMemeOrderAsync(
        MemeOrder memeOrder,
        CancellationToken cancellationToken = default
    );
    Task<bool> DeleteMemeOrderAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MemeOrder?> GetRandomMemeAsync(
        int? typeId = null,
        CancellationToken cancellationToken = default
    );
    Task<int> GetMemeOrderCountAsync(
        int? typeId = null,
        CancellationToken cancellationToken = default
    );
    Task ReorderMemeOrdersAsync(int typeId, CancellationToken cancellationToken = default);
}
