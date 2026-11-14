using System.Text.Json;
using Google.Protobuf;
using MARS.Modules.Alerts.Application.Adhd.Models;
using MARS.Shared.Contracts.Telegramus;

namespace MARS.Alerts.Presentation.Overlay.Hubs;

/// <summary>
/// Раскладка ADHD-экрана в том виде, в каком она едет клиенту.
/// </summary>
/// <remarks>
/// <para>
/// Полезная нагрузка — <c>bytes</c>: так же, как остальные объектные поля
/// контракта оверлея, настройка едет JSON'ом внутри события. Клиент разбирает
/// её тем же кодом, что и раньше, поэтому форма события не меняется.
/// </para>
/// <para>
/// Имена свойств — camelCase: ровно их ждёт разбор на клиенте и ровно их
/// отдавал броадкастер, пока настройка шла через gRPC.
/// </para>
/// </remarks>
internal static class AdhdConfigPayload
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Собирает событие с раскладкой.
    /// </summary>
    public static AdhdConfigEvent ToEvent(AdhdLayoutConfigDto config) =>
        new() { ConfigJson = ByteString.CopyFromUtf8(JsonSerializer.Serialize(config, Options)) };
}
