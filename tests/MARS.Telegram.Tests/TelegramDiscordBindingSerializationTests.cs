using System.Text.Json;
using MARS.Telegram.Entities;

namespace MARS.Telegram.Tests;

/// <summary>
/// Discord-идентификатор обязан переживать JSON без потери точности.
/// </summary>
/// <remarks>
/// <para>
/// Snowflake занимает 64 бита, а <c>Number</c> в браузере — 53. Отправленный
/// числом идентификатор округлялся бы молча: клиент создавал бы привязку к
/// соседнему каналу и не получал бы об этом никакого сообщения.
/// </para>
/// <para>
/// Проверяется именно то, из-за чего значение ушло в строковую форму: чтение из
/// строки и запись в строку. Раньше стоял только <c>WriteAsString</c>, который
/// разрешает запись, но не чтение, — запрос со значением в кавычках отклонялся
/// кодом 400 ещё до входа в контроллер.
/// </para>
/// </remarks>
public class TelegramDiscordBindingSerializationTests
{
    /// <summary>
    /// Значение, которое не помещается в <c>Number</c>.
    /// </summary>
    /// <remarks>
    /// Взято не случайное: в двоичном виде у него младшие разряды ненулевые, и
    /// округление до 53 бит даёт <c>1234567890123456800</c> — то есть ошибку
    /// видно на глаз.
    /// </remarks>
    private const ulong Snowflake = 1234567890123456789;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void RequestReadsFromStringWithoutRounding()
    {
        // Числовой литерал в тесте — это намеренное напоминание: такой JSON
        // приходит от старых версий клиента, и он обязан быть принят.
        const ulong RoundedValue = 1234567890123456800;

        var fromString = JsonSerializer.Deserialize<TelegramDiscordBindingCreateRequest>(
            "{\"telegramChannelId\":-1001234567890,\"discordChannelId\":\"1234567890123456789\"}",
            Options
        );
        var fromNumber = JsonSerializer.Deserialize<TelegramDiscordBindingCreateRequest>(
            "{\"telegramChannelId\":-1001234567890,\"discordChannelId\":1234567890123456789}",
            Options
        );

        Assert.NotNull(fromString);
        Assert.NotNull(fromNumber);

        Assert.Equal(Snowflake, fromString!.DiscordChannelId);
        Assert.Equal(Snowflake, fromNumber!.DiscordChannelId);

        // Показывает цену числовой формы: именно столько пришлось бы отправить
        // клиенту, оставив тип числа.
        Assert.NotEqual(RoundedValue, Snowflake);
    }

    [Fact]
    public void ResponseSerializesIdentifierAsString()
    {
        var dto = new TelegramDiscordBindingDto { DiscordChannelId = Snowflake };

        var json = JsonSerializer.Serialize(dto, Options);

        Assert.Contains(
            "\"discordChannelId\":\"1234567890123456789\"",
            json,
            StringComparison.Ordinal
        );

        // Строковое значение обязано разбираться обратно в то же значение —
        // иначе ответ нельзя было бы прочитать тем же кодом, который его принял.
        var readBack = JsonSerializer.Deserialize<TelegramDiscordBindingDto>(json, Options);

        Assert.NotNull(readBack);
        Assert.Equal(Snowflake, readBack!.DiscordChannelId);
    }
}
