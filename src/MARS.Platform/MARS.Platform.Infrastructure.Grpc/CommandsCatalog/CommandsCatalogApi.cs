using System.Diagnostics.CodeAnalysis;
using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.Commands.Catalog;

namespace MARS.Platform.Infrastructure.Grpc.CommandsCatalog;

[ExcludeFromCodeCoverage]
internal sealed class CommandsCatalogApi(GrpcClientFactory clientFactory) : ICommandsCatalogApi
{
    public async Task<TelegramCommandsResponse> GetTelegramCommandsAsync(
        string userId,
        bool isAdmin,
        IReadOnlyList<string> roles,
        string languageCode,
        CancellationToken cancellationToken = default
    )
    {
        var request = new GetTelegramCommandsRequest
        {
            UserId = userId,
            IsAdmin = isAdmin,
            LanguageCode = languageCode ?? string.Empty,
        };
        request.Roles.AddRange(roles ?? Array.Empty<string>());

        return await clientFactory
            .CreateClient<CommandsCatalogService.CommandsCatalogServiceClient>("CommandsCatalog")
            .GetTelegramCommandsAsync(request, cancellationToken: cancellationToken)
            .ResponseAsync;
    }

    public async Task<InlineCommandsResponse> SearchInlineCommandsAsync(
        string userId,
        bool isAdmin,
        IReadOnlyList<string> roles,
        string query,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var request = new SearchInlineCommandsRequest
        {
            UserId = userId,
            IsAdmin = isAdmin,
            Query = query ?? string.Empty,
            Limit = limit,
        };
        request.Roles.AddRange(roles ?? Array.Empty<string>());

        return await clientFactory
            .CreateClient<CommandsCatalogService.CommandsCatalogServiceClient>("CommandsCatalog")
            .SearchInlineCommandsAsync(request, cancellationToken: cancellationToken)
            .ResponseAsync;
    }
}
