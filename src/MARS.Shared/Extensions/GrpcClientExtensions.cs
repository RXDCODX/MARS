using System.Reflection;
using MARS.Shared.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Extensions;

/// <summary>
/// Регистрация клиентских каналов межсервисного gRPC.
///
/// Адрес gRPC **не хранится в <c>ServiceEndpoints</c> отдельным свойством:
/// Swagger-агрегатор строит карту рефлексией по строковым свойствам и приписывает
/// каждому <c>/swagger/v1/swagger.json</c>, поэтому свойство вида
/// <c>CommandsGrpc</c> заставило бы агрегатор лезть за спекой на порт, который
/// обслуживает только HTTP/2. Адрес выводится из адреса REST подменой порта.
/// </summary>
public static class GrpcClientExtensions
{
    /// <summary>
    /// Регистрирует типизированного gRPC-клиента на базовом адресе сервиса.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="baseAddress">
    /// Адрес сервиса в формате <c>http://имя-сервиса:8080</c> — как в
    /// <c>ServiceEndpoints</c>. Порт подставляется принудительно.
    /// </param>
    public static IHttpClientBuilder AddMarsGrpcClient<TClient>(
        this IServiceCollection services,
        string baseAddress
    )
        where TClient : class
    {
        var address = WithGrpcPort(baseAddress);

        return services.AddGrpcClient<TClient>(client => client.Address = address);
    }

    /// <summary>
    /// Регистрирует типизированного gRPC-клиента по имени свойства в
    /// <see cref="ServiceEndpoints"/>. Свойство ищется рефлексией, а не
    /// <c>switch</c>-ом: <c>switch</c> пришлось бы расширять руками, и забытое
    /// свойство выдавало бы исключение в рантайме вместо отсутствия.
    /// </summary>
    public static IHttpClientBuilder AddMarsGrpcClient<TClient>(
        this IServiceCollection services,
        IOptions<ServiceEndpoints> endpoints,
        string endpointName
    )
        where TClient : class
    {
        return services.AddMarsGrpcClient<TClient>(Resolve(endpoints, endpointName));
    }

    /// <summary>
    /// Меняет порт адреса на порт gRPC-эндпоинта из
    /// <see cref="GrpcHostingExtensions"/>, сохраняя схему и имя хоста и отбрасывая
    /// путь с запросом: базовый адрес клиента склеивается с путём метода, и любой
    /// путь в настройке дал бы 404.
    /// </summary>
    public static Uri WithGrpcPort(string baseAddress)
    {
        if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var parsed))
        {
            throw new ArgumentException(
                $"Адрес сервиса '{baseAddress}' не является абсолютным URL",
                nameof(baseAddress)
            );
        }

        return new UriBuilder(parsed.Scheme, parsed.Host, GrpcHostingExtensions.GrpcPort).Uri;
    }

    private static string Resolve(IOptions<ServiceEndpoints> endpoints, string endpointName)
    {
        var property = typeof(ServiceEndpoints)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Name, endpointName, StringComparison.OrdinalIgnoreCase)
                && candidate.PropertyType == typeof(string)
            );

        if (property is null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endpointName),
                endpointName,
                $"В {nameof(ServiceEndpoints)} нет строкового свойства с таким именем"
            );
        }

        var value = property.GetValue(endpoints.Value) as string;

        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException(
                $"Свойство {nameof(ServiceEndpoints)}.{endpointName} не задано"
            );
    }
}
