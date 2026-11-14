using MARS.Shared.Contracts.Clients;

namespace MARS.Shikimori.Application.Ports;

/// <summary>Прикладной порт для сохранения истории и чтения локального каталога.</summary>
public interface IShikimoriCatalog
{
    Task<ShikimoriCharacterRef> SaveCharacterAsync(
        ShikimoriCharacterRef character,
        CancellationToken cancellationToken = default
    );

    Task RecordPickAsync(
        string kind,
        ShikimoriTitleRef title,
        CancellationToken cancellationToken = default
    );

    Task<ShikimoriCharacterRef?> FindCharacterAsync(
        long id,
        CancellationToken cancellationToken = default
    );
}
