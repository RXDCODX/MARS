using System.Text.Json;
using MARS.Shared.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MARS.Shared.Tests.SignalR;

/// <summary>
/// JSON-протокол SignalR.
/// </summary>
/// <remarks>
/// <para>
/// Настройка через <c>Configure&lt;JsonOptions&gt;</c> в <c>AddMarsDefaults</c>
/// действует только на MVC. Хабы её не видят: у них свой
/// <c>PayloadSerializerOptions</c>, и по умолчанию перечисления в нём едут
/// числами.
/// </para>
/// <para>
/// Отсюда был отказ: клиент сравнивает
/// <c>playerState.state === PlayerStateStateEnum.Playing</c>, то есть ждёт имя,
/// а приходило <c>1</c>. Экран видео оставался в состоянии паузы при играющем
/// плеере, и по симптомам это ничем не отличалось от «плеер сломан».
/// </para>
/// </remarks>
public class SignalRJsonProtocolTests
{
    /// <summary>Состояние плеера: значения как в proto.</summary>
    private enum PlayerStateStateEnum
    {
        Stopped = 0,
        Playing = 1,
        Paused = 2,
    }

    /// <summary>Полезная нагрузка, похожая на снимок состояния плеера.</summary>
    private sealed class PlayerStateSnapshot
    {
        public PlayerStateStateEnum State { get; init; }

        public int Volume { get; init; }
    }

    /// <summary>
    /// Перечисление едет именем, а не числом.
    /// </summary>
    [Fact]
    public void Enums_are_serialized_by_name()
    {
        var serializer = CreatePayloadSerializerOptions();

        var json = JsonSerializer.Serialize(
            new PlayerStateSnapshot { State = PlayerStateStateEnum.Playing, Volume = 80 },
            serializer
        );

        Assert.Contains("\"Playing\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"state\":1", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// Имена свойств едут в camelCase.
    /// </summary>
    /// <remarks>
    /// Клиент читает <c>currentQueueItem</c> и <c>animationDuration</c>. Без
    /// camelCase пришлось бы читать <c>CurrentQueueItem</c> — другой JSON, чем
    /// у ответов REST, где правило настроено отдельно через MVC.
    /// </remarks>
    [Fact]
    public void Property_names_are_camel_case()
    {
        var serializer = CreatePayloadSerializerOptions();

        var json = JsonSerializer.Serialize(new PlayerStateSnapshot(), serializer);

        Assert.Contains("\"state\":", json, StringComparison.Ordinal);
        Assert.Contains("\"volume\":", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// Потолок входящего сообщения поднят: оверлей шлёт крупные объекты.
    /// </summary>
    /// <remarks>
    /// У SignalR по умолчанию 32 КБ. Стандартного потолка не хватало бы спискам
    /// призов и пачкам сообщений чата: клиент получал бы ошибку про размер
    /// вместо работающего экрана.
    /// </remarks>
    [Fact]
    public void Receive_message_size_limit_is_raised()
    {
        var options = CreateHubOptions();

        Assert.Equal(
            SignalRServiceCollectionExtensions.MaximumReceiveMessageSize,
            options.MaximumReceiveMessageSize
        );
        Assert.True(
            options.MaximumReceiveMessageSize > 32 * 1024,
            "Потолок SignalR по умолчанию — 32 КБ, этого мало оверлею."
        );
    }

    private static JsonHubProtocolOptions CreateProtocolOptions()
    {
        var options = new JsonHubProtocolOptions();

        SignalRServiceCollectionExtensions.ConfigureJsonProtocol(options);

        return options;
    }

    private static JsonSerializerOptions CreatePayloadSerializerOptions() =>
        CreateProtocolOptions().PayloadSerializerOptions;

    private static HubOptions CreateHubOptions()
    {
        var services = new ServiceCollection();

        services.AddMarsSignalR();

        return services
            .BuildServiceProvider()
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<HubOptions>>()
            .Value;
    }
}
