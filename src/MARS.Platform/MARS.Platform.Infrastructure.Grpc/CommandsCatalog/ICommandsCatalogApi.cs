using System.Diagnostics.CodeAnalysis;
using MARS.Shared.Contracts.Commands.Catalog;

namespace MARS.Platform.Infrastructure.Grpc.CommandsCatalog;

public interface ICommandsCatalogApi
{
    Task<TelegramCommandsResponse> GetTelegramCommandsAsync(
        string userId,
        bool isAdmin,
        IReadOnlyList<string> roles,
        string languageCode,
        CancellationToken cancellationToken = default
    );

    Task<InlineCommandsResponse> SearchInlineCommandsAsync(
        string userId,
        bool isAdmin,
        IReadOnlyList<string> roles,
        string query,
        int limit,
        CancellationToken cancellationToken = default
    );
}
