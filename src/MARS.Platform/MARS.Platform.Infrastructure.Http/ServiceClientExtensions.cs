using System;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Http;

/// <summary>
/// Совместимость со старыми регистрациями. Межсервисные HTTP-клиенты
/// перенесены на gRPC; здесь остаётся только поиск адреса по имени свойства
/// <see cref="ServiceEndpoints" /> для внешних/локальных адаптеров.
/// </summary>
public static class ServiceClientExtensions
{
    public static string GetServiceEndpoint(this IOptions<ServiceEndpoints> options, string name) =>
        name switch
        {
            nameof(ServiceEndpoints.MediaStorage) => options.Value.MediaStorage,
            nameof(ServiceEndpoints.WaifuGacha) => options.Value.WaifuGacha,
            nameof(ServiceEndpoints.Alerts) => options.Value.Alerts,
            nameof(ServiceEndpoints.OBS) => options.Value.OBS,
            nameof(ServiceEndpoints.Admin) => options.Value.Admin,
            nameof(ServiceEndpoints.TwitchCore) => options.Value.TwitchCore,
            nameof(ServiceEndpoints.Telegram) => options.Value.Telegram,
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Неизвестный сервис"),
        };
}
