using System.Text.Json;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Telegramus;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// В какой форме браузер получит полезную нагрузку события оверлея.
/// </summary>
/// <remarks>
/// Перенос компонентов клиента на типизированные подписки упирается в одну
/// вещь, которую нельзя вывести из кода, а можно только измерить: как именно
/// сериализуются ветки, у которых полезная нагрузка объявлена полем
/// <c>bytes</c> — это <c>message_json</c>, <c>user_json</c>, <c>gao_alert_json</c>
/// и подобные.
/// <para>
/// Ответ меняет всё написание клиента. По отображению protobuf поле
/// <c>bytes</c> кодируется base64-строкой, значит браузер получит
/// <c>messageJson: "eyJ0ZXh0Ijoi…"</c> и перед разбором обязан декодировать.
/// Если бы вместо строки пришёл объект, распаковка была бы лишней и вредной.
/// Поэтому проверка идёт по факту, а не по спецификации: тот же
/// <see cref="JsonSerializerOptions"/>, которым пользуется
/// <see cref="MarsGrpcJson"/>, и тот же протокол, которым SignalR пишет
/// сообщения (System.Text.Json с camelCase).
/// </para>
/// <para>
/// Тест ловит и вторую половину вопроса: имена полей. Ветка <c>oneof</c> едет
/// полем <c>newMessage</c>, и имя ветки — часть контракта. Браузер, который
/// ищет <c>new_message</c>, молча перестанет получать сообщения.
/// </para>
/// </remarks>
public class OverlayPayloadWireFormatTests
{
    /// <summary>
    /// Настройки, которыми пишет сервер.
    /// </summary>
    /// <remarks>
    /// Совпадают с <c>JsonSerializerDefaults.Web</c> из <c>MarsGrpcJson</c>:
    /// camelCase в именах и нечувствительность к регистру при чтении. Именно их
    /// использует протокол SignalR по умолчанию.
    /// </remarks>
    private static readonly JsonSerializerOptions WireFormat = new(JsonSerializerDefaults.Web);

    private static string Serialize(TelegramusEvent notification) =>
        JsonSerializer.Serialize(notification, WireFormat);

    /// <summary>
    /// Ветка с JSON внутри едет массивом байт, и разбирать её без декодера нельзя.
    /// </summary>
    /// <remarks>
    /// Формат измерен, а не взят из спецификации. Ожидание было обратным: по
    /// отображению protobuf поле <c>bytes</c> полагается base64-строкой, и код
    /// клиента, написанный на это предположение, получил бы вместо сообщения
    /// строку, из которой <c>JSON.parse</c> вернул бы мусор. Фактически
    /// <c>System.Text.Json</c> сериализует <c>ByteString</c> как массив чисел, и
    /// браузер обязан собрать из них <c>Uint8Array</c> и декодировать UTF-8.
    /// <para>
    /// Именно поэтому тест написан на фактическом выводе: догадка о формате стоила
    /// бы переноса всех компонентов оверлея вслепую.
    /// </para>
    /// </remarks>
    [Fact]
    public void Json_inside_bytes_branch_arrives_as_byte_array()
    {
        var notification = new TelegramusEvent
        {
            NewMessage = new NewMessageEvent
            {
                Id = "42",
                MessageJson = MarsGrpcJson.Serialize(new { text = "привет" }),
            },
        };

        using var document = JsonDocument.Parse(Serialize(notification));

        var messageJson = document.RootElement.GetProperty("newMessage").GetProperty("messageJson");

        // Массив байт, а не строка и не вложенный объект.
        Assert.Equal(JsonValueKind.Array, messageJson.ValueKind);

        var bytes = messageJson.EnumerateArray().Select(item => item.GetByte()).ToArray();

        using var inner = JsonDocument.Parse(System.Text.Encoding.UTF8.GetString(bytes));

        Assert.Equal("привет", inner.RootElement.GetProperty("text").GetString());
    }

    /// <summary>
    /// Незаполненные ветки <c>oneof</c> едут <c>null</c>, а не отсутствуют.
    /// </summary>
    /// <remarks>
    /// Из этого следует, что клиент не может определять событие по наличию
    /// ключа: ключ есть всегда, у всех 36 веток. Различать надо по
    /// <c>eventCase</c> либо по тому, какое значение не <c>null</c>.
    /// <para/>
    /// Побочно это раздувает сообщение: на каждое событие едет около
    /// тридцати пяти ключей с <c>null</c>. При всплеске наград на
    /// <c>/hubs/overlay</c> это лишний трафик, и об этом стоит помнить при
    /// оптимизации, а не при исправлении клиента.
    /// </remarks>
    [Fact]
    public void Unset_oneof_branches_are_present_as_null()
    {
        var notification = new TelegramusEvent { Explosion = new EmptyEvent() };

        using var document = JsonDocument.Parse(Serialize(notification));

        // Заполненная ветка — объект.
        Assert.Equal(JsonValueKind.Object, document.RootElement.GetProperty("explosion").ValueKind);

        // Незаполненные ветки при этом никуда не делись.
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("newMessage").ValueKind);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("allRefund").ValueKind);

        // Событие опознаётся номером ветки, а не наличием ключа.
        Assert.Equal(TelegramusEvent.EventOneofCase.Explosion, notification.EventCase);
    }

    /// <summary>
    /// Идентификатор едет обычной строкой, и разбор обязан различать эти два
    /// случая: иначе идентификатор превратится в мусор, и сообщения перестанут
    /// удаляться.
    /// </summary>
    [Fact]
    public void Plain_string_branch_field_is_not_base64()
    {
        var notification = new TelegramusEvent
        {
            NewMessage = new NewMessageEvent
            {
                Id = "42",
                MessageJson = MarsGrpcJson.Serialize(new { text = "x" }),
            },
        };

        using var document = JsonDocument.Parse(Serialize(notification));

        var id = document.RootElement.GetProperty("newMessage").GetProperty("id");

        Assert.Equal(JsonValueKind.String, id.ValueKind);
        Assert.Equal("42", id.GetString());
    }

    /// <summary>
    /// Число секунд едет числом, а не строкой: оверлей ставит таймер по нему,
    /// и строковое «30» вместо числа означало бы таймер без интервала.
    /// </summary>
    [Fact]
    public void Numeric_branch_field_arrives_as_number()
    {
        var notification = new TelegramusEvent { Adhd = new AdhdEvent { Seconds = 30 } };

        using var document = JsonDocument.Parse(Serialize(notification));

        var seconds = document.RootElement.GetProperty("adhd").GetProperty("seconds");

        Assert.Equal(JsonValueKind.Number, seconds.ValueKind);
        Assert.Equal(30, seconds.GetInt32());
    }

    /// <summary>
    /// Имя ветки <c>oneof</c> едет в camelCase и служит признаком наличия
    /// события: клиент по нему и решает, что именно разбирать.
    /// </summary>
    [Fact]
    public void Oneof_branch_names_are_camel_case()
    {
        var notification = new TelegramusEvent { GaoAlert = new GaoAlertEvent() };

        using var document = JsonDocument.Parse(Serialize(notification));

        Assert.True(
            document.RootElement.TryGetProperty("gaoAlert", out _),
            document.RootElement.ToString()
        );
        Assert.False(document.RootElement.TryGetProperty("gao_alert", out _));
    }

    /// <summary>
    /// Ветка без данных едет пустым объектом — это отличает «событие без
    /// полезной нагрузки» от «события не было вовсе».
    /// </summary>
    [Fact]
    public void Empty_branch_arrives_as_empty_object()
    {
        var notification = new TelegramusEvent { Explosion = new EmptyEvent() };

        using var document = JsonDocument.Parse(Serialize(notification));

        var branch = document.RootElement.GetProperty("explosion");

        Assert.Equal(JsonValueKind.Object, branch.ValueKind);
        Assert.Empty(branch.EnumerateObject().ToArray());
    }
}
