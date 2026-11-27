using Grpc.Net.ClientFactory;
using MARS.Platform.Infrastructure.Grpc.Alerts;
using MARS.Platform.Infrastructure.Grpc.Authentication;
using MARS.Platform.Infrastructure.Grpc.Commands;
using MARS.Platform.Infrastructure.Grpc.CommandsCatalog;
using MARS.Platform.Infrastructure.Grpc.Discord;
using MARS.Platform.Infrastructure.Grpc.MediaStorage;
using MARS.Platform.Infrastructure.Grpc.Shikimori;
using MARS.Platform.Infrastructure.Grpc.Twitch;
using MARS.Platform.Infrastructure.Grpc.WaifuGacha;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Alerts.Control;
using MARS.Shared.Contracts.Commands.Catalog;
using MARS.Shared.Contracts.Commands.Control;
using MARS.Shared.Contracts.Discord.Control;
using MARS.Shared.Contracts.MediaStorage;
using MARS.Shared.Contracts.Shikimori;
using MARS.Shared.Contracts.SoundRequest.Control;
using MARS.Shared.Contracts.TelegramAdmin;
using MARS.Shared.Contracts.TelegramLog;
using MARS.Shared.Contracts.TelegramMessage;
using MARS.Shared.Contracts.Tts.Control;
using MARS.Shared.Contracts.Twitch.Control;
using MARS.Shared.Contracts.WaifuGacha.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Grpc.Extensions;

public static class GrpcClientExtensions
{
    public static IServiceCollection AddMarsGrpcClient(this IServiceCollection services)
    {
        services.AddSingleton<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<AlertsControlService.AlertsControlServiceClient>(
                "Alerts",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "Alerts"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<CommandsControlService.CommandsControlServiceClient>(
                "Commands",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "Commands"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<CommandsCatalogService.CommandsCatalogServiceClient>(
                "CommandsCatalog",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "Commands"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<WaifuGachaService.WaifuGachaServiceClient>(
                "WaifuGacha",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "WaifuGacha"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<TwitchControlService.TwitchControlServiceClient>(
                "TwitchCore",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "TwitchCore"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<ShikimoriService.ShikimoriServiceClient>(
                "Shikimori",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "Shikimori"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<MediaStorageService.MediaStorageServiceClient>(
                "MediaStorage",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "MediaStorage"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<SoundRequestControlService.SoundRequestControlServiceClient>(
                "SoundRequest",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "SoundRequest"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<TtsControlService.TtsControlServiceClient>(
                "Tts",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "Tts"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<DiscordMessageService.DiscordMessageServiceClient>(
                "Discord",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "Discord"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<TelegramAdminService.TelegramAdminServiceClient>(
                "TelegramAdmin",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "Telegram"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<TelegramLogService.TelegramLogServiceClient>(
                "TelegramLog",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "Telegram"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services
            .AddGrpcClient<TelegramMessageService.TelegramMessageServiceClient>(
                "TelegramMessage",
                (serviceProvider, options) =>
                {
                    var endpoints = serviceProvider
                        .GetRequiredService<IOptions<GrpcOptions>>()
                        .Value;
                    options.Address = new Uri(ResolveEndpoint(endpoints, "Telegram"));
                }
            )
            .AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services.AddSingleton<IMarsGrpcClient, MarsGrpcClient>();

        return services;
    }

    private static string ResolveEndpoint(GrpcOptions options, string name)
    {
        if (
            options.Endpoints.TryGetValue(name, out var endpoint)
            && !string.IsNullOrWhiteSpace(endpoint)
        )
        {
            return endpoint;
        }

        throw new InvalidOperationException($"gRPC endpoint '{name}' is not configured.");
    }
}
