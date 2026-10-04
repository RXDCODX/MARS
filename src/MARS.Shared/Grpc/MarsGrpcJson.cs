using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;

namespace MARS.Shared.Grpc;

public static class MarsGrpcJson
{
    /// <summary>
    /// Настройки сериализации байтовых полей протокола.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Через эти настройки идут все поля <c>bytes</c> в <c>telegramus.proto</c>,
    /// поэтому здесь собрано то же, что и в трёх других местах репозитория:
    /// camelCase в именах полей (из <c>JsonSerializerDefaults.Web</c>) и имена
    /// перечислений (из <see cref="JsonStringEnumConverter"/>). Клиент сравнивает
    /// перечисления строками во всех местах, где их читает.
    /// </para>
    /// <para>
    /// Конвертера здесь не было, и это был единственный путь, где он отсутствовал:
    /// MVC, хабы и межсервисный HTTP его ставят, а байтовые поля — нет. Из-за
    /// этого <c>TwitchScreenParticles.Confetty</c> ехал числом <c>0</c>, а
    /// клиент сравнивал его со строкой: ни одна ветка <c>switch</c> не
    /// сходилась, и экран частиц не рисовал ничего.
    /// </para>
    /// </remarks>
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }

    public static ByteString Serialize(object? value)
    {
        if (value is null)
        {
            return ByteString.Empty;
        }

        return ByteString.CopyFrom(
            JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), Options)
        );
    }
}
