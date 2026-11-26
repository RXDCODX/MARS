using Grpc.Net.ClientFactory;
using MARS.Platform.Infrastructure.Grpc.Authentication;
using Microsoft.Extensions.DependencyInjection;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.TelegramAdmin;
using MARS.Shared.Contracts.TelegramLog;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Grpc.Extensions;

public static class GrpcClientExtensions
{
    public static IServiceCollection AddMarsGrpcClient(this IServiceCollection services)
    {
        services.AddSingleton<ServiceApiKeyGrpcClientInterceptor>();

        services.AddGrpcClient<TelegramAdminService.TelegramAdminServiceClient>(
            "TelegramAdmin",
            (serviceProvider, options) =>
            {
                var endpoints = serviceProvider.GetRequiredService<IOptions<GrpcOptions>>().Value;
                options.Address = new Uri(ResolveEndpoint(endpoints, "Telegram"));
            }
        ).AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services.AddGrpcClient<TelegramLogService.TelegramLogServiceClient>(
            "TelegramLog",
            (serviceProvider, options) =>
            {
                var endpoints = serviceProvider.GetRequiredService<IOptions<GrpcOptions>>().Value;
                options.Address = new Uri(ResolveEndpoint(endpoints, "Telegram"));
            }
        ).AddInterceptor<ServiceApiKeyGrpcClientInterceptor>();

        services.AddSingleton<IMarsGrpcClient, MarsGrpcClient>();

        return services;
    }

    private static string ResolveEndpoint(GrpcOptions options, string name)
    {
        if (options.Endpoints.TryGetValue(name, out var endpoint) && !string.IsNullOrWhiteSpace(endpoint))
        {
            return endpoint;
        }

        throw new InvalidOperationException($"gRPC endpoint '{name}' is not configured.");
    }
}