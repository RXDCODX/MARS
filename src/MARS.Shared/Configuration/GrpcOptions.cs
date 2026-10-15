namespace MARS.Shared.Configuration;

/// <summary>
/// Порты gRPC-эндпоинта и карта адресов для локальной отладки.
///
/// Дефолты повторяют то, что объявлено кодом в
/// <c>GrpcHostingExtensions</c>: REST на 8080, gRPC на 8081 — ровно так это
/// работает в контейнерах. На хосте порты приходят из профиля запуска:
/// <c>ListenAnyIP</c> подавляет <c>ASPNETCORE_URLS</c>, поэтому без этой секции
/// локальный сервис занимал бы 8080/8081 и мешал соседям, а занятые порты
/// на машине разработчика — норма, там уже стоит стенд.
///
/// <see cref="Endpoints"/> нужен клиенту: адрес gRPC выводится из адреса REST
/// подменой порта, а на хосте номера не совпадают с контейнерными
/// (REST alerts — 6010, gRPC — 6028), и правило «порт + 1» дало бы адрес,
/// которого никто не слушает. Ключ карты — имя свойства в
/// <see cref="ServiceEndpoints"/>.
/// </summary>
public class GrpcOptions
{
    public const string SectionName = "Grpc";

    public int HttpPort { get; set; } = 8080;

    public int GrpcPort { get; set; } = 8081;

    public Dictionary<string, string> Endpoints { get; set; } = [];
}
