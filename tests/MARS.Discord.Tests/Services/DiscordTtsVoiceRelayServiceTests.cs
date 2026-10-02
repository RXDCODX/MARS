using System.Reflection;
using System.Text;
using DSharpPlus;
using MARS.Discord.Services.Gateway;
using MARS.Discord.Services.TtsVoiceRelay;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Discord.Tests.Services;

/// <summary>
/// Пересылка TTS в голосовой канал Discord.
///
/// Сервис включается, только когда заданный пользователь сидит в заданном voice-канале:
/// иначе бот говорил бы в пустой канал и наполнял гилдию шумом. Проверяется это
/// состояние (при отсутствии подключения — выключено), отмена монитора и разбор
/// WAV-буфера перед отправкой в голосовой канал.
///
/// Синтез речи не проверяется: он зависит от установленных в системе голосов и
/// занимает секунды, а проверять тут нужно разбор аудио, а не TTS.
/// </summary>
public class DiscordTtsVoiceRelayServiceTests
{
    private readonly Mock<IDiscordGatewayService> _gateway = new();
    private readonly DiscordTtsVoiceRelayService _service = new(
        Mock.Of<IDiscordGatewayService>(),
        NullLogger<DiscordTtsVoiceRelayService>.Instance
    );

    public DiscordTtsVoiceRelayServiceTests() =>
        _service = new DiscordTtsVoiceRelayService(
            _gateway.Object,
            NullLogger<DiscordTtsVoiceRelayService>.Instance
        );

    /// <summary>
    /// Обработчик смены состояния голоса регистрируется при старте: иначе
    /// переподключение пользователя осталось бы незамеченным, и бот продолжал бы
    /// говорить в пустой канал.
    /// </summary>
    [Fact]
    public async Task StartSubscribesToVoiceState()
    {
        await StartAsync();

        _gateway.Verify(
            instance =>
                instance.RegisterVoiceStateUpdatedHandler(
                    It.IsAny<
                        Func<DiscordClient, DSharpPlus.EventArgs.VoiceStateUpdatedEventArgs, Task>
                    >()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Без подключённого клиента маршрутизация выключена: лучше молчать, чем
    /// слать речь в никуда.
    /// </summary>
    [Fact]
    public async Task RoutingIsOffWithoutClient()
    {
        await StartAsync();

        Assert.False(_service.IsVoiceRoutingEnabled);
    }

    [Fact]
    public async Task StopTurnsRoutingOff()
    {
        await StartAsync();

        await _service.StopAsync(TestContext.Current.CancellationToken);

        Assert.False(_service.IsVoiceRoutingEnabled);
    }

    [Fact]
    public async Task StopBeforeStartIsSafe()
    {
        await _service.StopAsync(TestContext.Current.CancellationToken);

        Assert.False(_service.IsVoiceRoutingEnabled);
    }

    /// <summary>
    /// Выключенная маршрутизация проглатывает запрос молча: команда TTS не должна
    /// падать, когда бот не в голосовом канале.
    /// </summary>
    [Fact]
    public async Task SpeechIsSkippedWhenRoutingIsOff()
    {
        await _service.PlaySpeechAsync(
            "Алёна",
            "текст",
            cancellationToken: TestContext.Current.CancellationToken
        );

        _gateway.Verify(
            instance => instance.EnsureConnectedAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    /// <summary>
    /// WAV-обёртка снимается перед отправкой: Discord принимает только сырой PCM,
    /// а заголовок RIFF в поток ушёл бы лишними 44 байтами и дал бы треск в начале.
    /// </summary>
    [Fact]
    public void WaveHeaderIsStripped()
    {
        // Буфер длиннее 44 байт: короче заголовок с заголовком блока не различим,
        // и разбирать там нечего.
        var payload = Enumerable.Range(1, 40).Select(value => (byte)value).ToArray();
        var wave = Wave(payload);

        var result = (byte[])InvokeStatic("ExtractPcmPayload", wave);

        Assert.Equal(payload, result);
    }

    /// <summary>
    /// Сырой PCM без заголовка уходит как есть: разбирать там нечего, а
    /// «починка» испортила бы звук.
    /// </summary>
    [Fact]
    public void RawPcmIsNotChanged()
    {
        var pcm = new byte[] { 9, 8, 7 };

        Assert.Equal(pcm, (byte[])InvokeStatic("ExtractPcmPayload", pcm));
    }

    [Fact]
    public void TooShortBufferIsNotChanged()
    {
        var tiny = new byte[] { (byte)'R', (byte)'I' };

        Assert.Equal(tiny, (byte[])InvokeStatic("ExtractPcmPayload", tiny));
    }

    /// <summary>
    /// Блок данных ищется по идентификатору <c>data</c> с учётом выравнивания
    /// до чётной длины: без него заголовок искался бы не с того места.
    /// </summary>
    [Fact]
    public void DataChunkOffsetIsFoundAfterOtherChunks()
    {
        var wave = Wave(Enumerable.Repeat((byte)7, 40).ToArray(), FormatChunk());

        var offset = (int)InvokeStatic("FindDataChunkOffset", wave);

        Assert.True(offset > 0);
        Assert.All(
            (byte[])InvokeStatic("ExtractPcmPayload", wave),
            value => Assert.Equal(7, value)
        );
    }

    /// <summary>
    /// Минимальный WAV: заголовок RIFF/WAVE, необязательный блок формата и блок
    /// данных с самим PCM. Блок данных собирается по правилам RIFF — идентификатор
    /// <c>data</c>, размер и выравнивание до чётной границы, — иначе поиск смещения
    /// проверял бы не формат, а мою ошибку в сборке теста.
    /// </summary>
    private static byte[] Wave(byte[] payload, byte[]? formatChunk = null)
    {
        var body = new List<byte>();
        body.AddRange("WAVE"u8);

        if (formatChunk is not null)
        {
            body.AddRange(formatChunk);
        }

        var dataSize = payload.Length + (payload.Length % 2);
        body.AddRange("data"u8);
        body.AddRange(BitConverter.GetBytes(dataSize));
        body.AddRange(payload);
        if (payload.Length % 2 != 0)
        {
            body.Add(0);
        }

        var result = new List<byte>();
        result.AddRange("RIFF"u8);
        result.AddRange(BitConverter.GetBytes(body.Count));
        result.AddRange(body);

        return [.. result];
    }

    /// <summary>
    /// Блок формата <c>fmt </c> перед блоком данных: с ним смещение данных
    /// находится не сразу после заголовка, и поиск по <c>data</c> это проверяет.
    /// </summary>
    private static byte[] FormatChunk()
    {
        var result = new List<byte>();
        result.AddRange("fmt "u8);
        result.AddRange(BitConverter.GetBytes(16));
        result.AddRange([1, 0]);
        result.AddRange([1, 0]);
        result.AddRange(BitConverter.GetBytes(48000));
        result.AddRange(BitConverter.GetBytes(192000));
        result.AddRange([4, 0]);
        result.AddRange([16, 0]);

        return [.. result];
    }

    private Task StartAsync() => _service.StartAsync(TestContext.Current.CancellationToken);

    private static object InvokeStatic(string method, params object[] arguments) =>
        typeof(DiscordTtsVoiceRelayService)
            .GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, arguments)!;
}
