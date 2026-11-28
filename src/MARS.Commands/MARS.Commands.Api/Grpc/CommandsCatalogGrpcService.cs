using Grpc.Core;
using MARS.Commands.Application.Services;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using Entitys = MARS.Commands.Application.Services.Entitys;
using MARS.Shared.Contracts.Commands.Catalog;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Api.Grpc;

public sealed class CommandsCatalogGrpcService(ICommandService commandService) : CommandsCatalogService.CommandsCatalogServiceBase
{
    public override Task<TelegramCommandsResponse> GetTelegramCommands(
        GetTelegramCommandsRequest request,
        ServerCallContext context
    )
    {
        var userCommands = commandService.GetUserCommandsInfo(Entitys.Platform.Telegram);
        var adminCommands = request.IsAdmin
            ? commandService.GetAdminCommandsInfo(Entitys.Platform.Telegram)
            : Array.Empty<BaseCommand>();

        var commands = userCommands
            .Concat(adminCommands)
            .DistinctBy(command => command.CommandName, StringComparer.OrdinalIgnoreCase)
            .Select(command => new TelegramCommand
            {
                Name = command.CommandName,
                Description = command.Description,
                Aliases = { command.Aliases },
                IsAdmin = command.IsAdminCommand,
                SupportsInline = true,
            })
            .ToList();

        return Task.FromResult(new TelegramCommandsResponse
        {
            Commands = { commands },
            CatalogVersion = "1",
        });
    }

    public override Task<InlineCommandsResponse> SearchInlineCommands(
        SearchInlineCommandsRequest request,
        ServerCallContext context
    )
    {
        var limit = request.Limit <= 0 ? 20 : Math.Min(request.Limit, 50);
        var query = request.Query?.Trim() ?? string.Empty;

        var candidates = commandService
            .GetUserCommandsInfo(Entitys.Platform.Telegram)
            .Concat(request.IsAdmin ? commandService.GetAdminCommandsInfo(Entitys.Platform.Telegram) : Array.Empty<BaseCommand>())
            .DistinctBy(command => command.CommandName, StringComparer.OrdinalIgnoreCase);

        var results = candidates
            .Where(command => Matches(command, query))
            .Take(limit)
            .Select(command => new InlineCommand
            {
                Id = command.CommandName,
                Title = command.InlineTitle,
                Description = command.InlineDescription,
                CommandText = $"/{command.CommandName}",
            })
            .ToList();

        return Task.FromResult(new InlineCommandsResponse
        {
            Results = { results },
            CacheTimeSeconds = 0,
        });
    }

    private static bool Matches(BaseCommand command, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return command.CommandName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || command.Aliases.Any(alias => alias.Contains(query, StringComparison.OrdinalIgnoreCase))
            || command.Description.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}