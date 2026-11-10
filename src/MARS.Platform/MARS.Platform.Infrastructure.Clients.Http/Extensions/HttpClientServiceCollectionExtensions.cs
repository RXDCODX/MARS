using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace MARS.Platform.Infrastructure.Clients.Http.Extensions;

public static class HttpClientServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// HTTP клиент к другому сервису с tracing и retry (Polly v8)
        /// </summary>
        public IServiceCollection AddMarsHttpClient<TClient>(string serviceName, string baseAddress)
            where TClient : class
        {
            services
                .AddHttpClient<TClient>(client =>
                {
                    client.BaseAddress = new Uri(baseAddress);
                    client.DefaultRequestHeaders.UserAgent.Add(
                        new ProductInfoHeaderValue("MARS", serviceName)
                    );
                })
                .AddStandardResilienceHandler();

            return services;
        }
    }
}
