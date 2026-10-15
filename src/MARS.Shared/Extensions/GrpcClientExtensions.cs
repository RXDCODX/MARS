using System.Reflection;
using MARS.Shared.Configuration;
using Microsoft.Extensions.Configuration;
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
    /// Регистрирует типизированного gRPC-клиента по имени сервиса, читая адреса
    /// из конфигурации: REST — из <see cref="ServiceEndpoints"/>, gRPC — из
    /// <see cref="GrpcOptions"/>. Этот способ единственный работает на хосте,
    /// где хостовые порты не совпадают с контейнерными.
    /// </summary>
    public static IHttpClientBuilder AddMarsGrpcClient<TClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        string endpointName
    )
        where TClient : class
    {
        var endpoints =
            configuration.GetSection(ServiceEndpoints.SectionName).Get<ServiceEndpoints>()
            ?? new ServiceEndpoints();
        var grpc =
            configuration.GetSection(GrpcOptions.SectionName).Get<GrpcOptions>()
            ?? new GrpcOptions();

        return services.AddMarsGrpcClient<TClient>(endpoints, endpointName, grpc);
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
        string endpointName,
        GrpcOptions? grpc = null
    )
        where TClient : class
    {
        return services.AddMarsGrpcClient<TClient>(endpoints.Value, endpointName, grpc);
    }

    private static IHttpClientBuilder AddMarsGrpcClient<TClient>(
        this IServiceCollection services,
        ServiceEndpoints endpoints,
        string endpointName,
        GrpcOptions? grpc
    )
        where TClient : class
    {
        var baseAddress = Resolve(endpoints, endpointName);
        var address = WithGrpcPort(baseAddress, grpc, endpointName);

        return services.AddGrpcClient<TClient>(client => client.Address = address);
    }

    /// <summary>
    /// Меняет порт адреса на порт gRPC-эндпоинта из
    /// <see cref="GrpcHostingExtensions"/>, сохраняя схему и имя хоста и отбрасывая
    /// путь с запросом: базовый адрес клиента склеивается с путём метода, и любой
    /// путь в настройке дал бы 404.
    /// </summary>
    public static Uri WithGrpcPort(string baseAddress)
    {
        return WithGrpcPort(baseAddress, null, null);
    }

    /// <summary>
    /// Выводит адрес gRPC с учётом <see cref="GrpcOptions"/>: если для сервиса
    /// задан явный адрес, берётся он, иначе — порт по умолчанию. Правило
    /// «порт + 1» здесь неприменимо: на хосте REST- и gRPC-порты разнесены
    /// по схеме и разница у каждого сервиса своя.
    /// </summary>
    public static Uri WithGrpcPort(string baseAddress, GrpcOptions? grpc, string? endpointName)
    {
        if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var parsed))
        {
            throw new ArgumentException(
                $"Адрес сервиса '{baseAddress}' не является абсолютным URL",
                nameof(baseAddress)
            );
        }

        var result = default(Uri);

        if (grpc is not null && !string.IsNullOrEmpty(endpointName))
        {
            var configured = grpc
                .Endpoints.FirstOrDefault(pair =>
                    string.Equals(pair.Key, endpointName, StringComparison.OrdinalIgnoreCase)
                )
                .Value;

            if (
                !string.IsNullOrWhiteSpace(configured)
                && Uri.TryCreate(configured, UriKind.Absolute, out var mapped)
            )
            {
                result = new UriBuilder(mapped.Scheme, mapped.Host, mapped.Port).Uri;
            }
        }

        result ??= new UriBuilder(
            parsed.Scheme,
            parsed.Host,
            grpc?.GrpcPort ?? GrpcHostingExtensions.GrpcPort
        ).Uri;

        return result;
    }

    private static string Resolve(ServiceEndpoints endpoints, string endpointName)
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

        var value = property.GetValue(endpoints) as string;

        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException(
                $"Свойство {nameof(ServiceEndpoints)}.{endpointName} не задано"
            );
    }
}
