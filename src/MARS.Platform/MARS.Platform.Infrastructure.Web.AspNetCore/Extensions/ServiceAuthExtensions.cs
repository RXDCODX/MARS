using MARS.Platform.Infrastructure.Web.AspNetCore.Authentication;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;

/// <summary>
/// Регистрация межсервисной аутентификации по API-ключу.
/// Схема включается автоматически, если в конфигурации задан <c>ServiceAuth:ApiKey</c>.
/// </summary>
public static class ServiceAuthExtensions
{
    public const string PolicyName = "ServiceApiKey";

    public static IServiceCollection AddMarsAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<ServiceAuthOptions>(
            configuration.GetSection(ServiceAuthOptions.SectionName)
        );

        services
            .AddAuthentication(ServiceApiKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ServiceApiKeyAuthenticationHandler>(
                ServiceApiKeyAuthenticationHandler.SchemeName,
                _ => { }
            );

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                PolicyName,
                policy =>
                {
                    policy.AddAuthenticationSchemes(ServiceApiKeyAuthenticationHandler.SchemeName);
                    policy.RequireAuthenticatedUser();
                }
            );
        });

        return services;
    }
}
