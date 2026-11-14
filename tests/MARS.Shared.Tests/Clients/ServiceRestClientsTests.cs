using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Tests.Clients;

/// <summary>
/// Тонкие HTTP-клиенты, заменившие gRPC-вызовы между сервисами.
/// </summary>
/// <remarks>
/// Проверяется то, на чём держится замена: адрес эндпоинта, ключ межсервисной
/// аутентификации, разбор конверта <c>OperationResult&lt;T&gt;</c> и поведение
/// при отказе сервиса. Транспорт подменён заглушкой: настоящий запрос к живому
/// сервису проверял бы сеть, а не клиент.
/// </remarks>
public class ServiceRestClientsTests
{
    private static readonly ServiceEndpoints Endpoints = new();

    [Fact]
    public async Task Клиент_Администраторов_Шлёт_Ключ_И_Читает_Итог()
    {
        var handler = new StubHttpHandler(
            HttpStatusCode.OK,
            """{"success":true,"result":{"recipients":2}}"""
        );
        var client = new TelegramAdminClient(
            HttpClient(handler, Endpoints.Telegram),
            Options.Create(new ServiceAuthOptions { ApiKey = "secret" }),
            Options.Create(Endpoints),
            NullLogger<TelegramAdminClient>.Instance
        );

        var result = await client.SendToAdminsAsync(
            "MARS.TwitchCore",
            "ссылка",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(result);
        Assert.Equal(2, result!.Recipients);
        Assert.Equal(
            "http://telegram:8080/api/internal/telegram/admin-messages",
            handler.Requests.Single().RequestUri!.ToString()
        );
        Assert.Equal("secret", handler.Requests.Single().Headers.GetValues("X-Api-Key").Single());
        Assert.Contains(
            "\"service\":\"MARS.TwitchCore\"",
            handler.Bodies.Single(),
            System.StringComparison.Ordinal
        );
    }

    /// <summary>
    /// Ненастроенный Telegram отвечает 412; для вызывающего это «доставить не
    /// удалось», а не исключение: просьба пройти авторизацию не должна ронять
    /// фоновую службу.
    /// </summary>
    [Fact]
    public async Task Клиент_Администраторов_Возвращает_Null_При_Отказе()
    {
        var handler = new StubHttpHandler(
            HttpStatusCode.PreconditionFailed,
            """{"success":false,"errorMessage":"Telegram не настроен"}"""
        );
        var client = new TelegramAdminClient(
            HttpClient(handler, Endpoints.Telegram),
            Options.Create(new ServiceAuthOptions { ApiKey = "secret" }),
            Options.Create(Endpoints),
            NullLogger<TelegramAdminClient>.Instance
        );

        var result = await client.SendToAdminsAsync(
            "MARS.TwitchCore",
            "ссылка",
            TestContext.Current.CancellationToken
        );

        Assert.Null(result);
    }

    /// <summary>
    /// Событие оверлея — уже готовый JSON protobuf-сообщения, поэтому оно уходит
    /// телом как есть. Повторная сериализация завернула бы его в строку, и
    /// protobuf на принимающей стороне не разобрал бы тело.
    /// </summary>
    [Fact]
    public async Task Клиент_Оверлея_Шлёт_Готовый_Json_Телом()
    {
        const string eventJson = """{"explosion":{}}""";
        var handler = new StubHttpHandler(
            HttpStatusCode.OK,
            """{"success":true,"result":{"subscribers":1}}"""
        );
        var client = new OverlayEventsClient(
            HttpClient(handler, Endpoints.Alerts),
            Options.Create(new ServiceAuthOptions { ApiKey = "secret" }),
            Options.Create(Endpoints),
            NullLogger<OverlayEventsClient>.Instance
        );

        var delivered = await client.FireAsync(eventJson, TestContext.Current.CancellationToken);

        Assert.True(delivered);
        Assert.Equal(eventJson, handler.Bodies.Single());
        Assert.Equal(
            "http://alerts:8080/api/internal/overlay/events",
            handler.Requests.Single().RequestUri!.ToString()
        );
    }

    [Fact]
    public async Task Клиент_TTS_Шлёт_Состояние_Глушения()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        var client = new TtsControlClient(
            HttpClient(handler, Endpoints.TTS),
            Options.Create(new ServiceAuthOptions { ApiKey = "secret" }),
            Options.Create(Endpoints),
            NullLogger<TtsControlClient>.Instance
        );

        var applied = await client.SetMuteStateAsync(
            true,
            0,
            TestContext.Current.CancellationToken
        );

        Assert.True(applied);
        Assert.Equal(
            "http://tts:8080/api/internal/tts/mute-state",
            handler.Requests.Single().RequestUri!.ToString()
        );
        Assert.Contains(
            "\"isMuted\":true",
            handler.Bodies.Single(),
            System.StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(true, "http://sound-request:8080/api/internal/sound-request/mute")]
    [InlineData(false, "http://sound-request:8080/api/internal/sound-request/unmute")]
    public async Task Клиент_Плеера_Шлёт_Глушение_И_Снятие(bool mute, string expectedUrl)
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        var client = new SoundRequestControlClient(
            HttpClient(handler, Endpoints.SoundRequest),
            Options.Create(new ServiceAuthOptions { ApiKey = "secret" }),
            Options.Create(Endpoints),
            NullLogger<SoundRequestControlClient>.Instance
        );

        var applied = mute
            ? await client.MuteAllAsync(TestContext.Current.CancellationToken)
            : await client.UnmuteAllAsync(TestContext.Current.CancellationToken);

        Assert.True(applied);
        Assert.Equal(expectedUrl, handler.Requests.Single().RequestUri!.ToString());
    }

    /// <summary>
    /// Пустой ключ — одиночный локальный запуск: схема аутентификации выключена,
    /// и заголовок не отправляется вовсе.
    /// </summary>
    [Fact]
    public async Task Без_Ключа_Заголовок_Не_Отправляется()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        var client = new SoundRequestControlClient(
            HttpClient(handler, Endpoints.SoundRequest),
            Options.Create(new ServiceAuthOptions()),
            Options.Create(Endpoints),
            NullLogger<SoundRequestControlClient>.Instance
        );

        await client.MuteAllAsync(TestContext.Current.CancellationToken);

        Assert.False(handler.Requests.Single().Headers.Contains("X-Api-Key"));
    }

    [Fact]
    public void Клиенты_Регистрируются_И_Разрешаются_Контейнером()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.Configure<ServiceEndpoints>(_ => { });
        services.AddMarsTelegramAdminClient();
        services.AddMarsOverlayEventsClient();
        services.AddMarsSoundControlClients();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ITelegramAdminClient>());
        Assert.NotNull(provider.GetRequiredService<IOverlayEventsClient>());
        Assert.NotNull(provider.GetRequiredService<ITtsControlClient>());
        Assert.NotNull(provider.GetRequiredService<ISoundRequestControlClient>());
    }

    private static HttpClient HttpClient(HttpMessageHandler handler, string baseAddress) =>
        new(handler) { BaseAddress = new System.Uri(baseAddress) };

    /// <summary>
    /// Заглушка транспорта: запоминает запрос и отвечает заданным конвертом.
    /// </summary>
    private sealed class StubHttpHandler(HttpStatusCode statusCode, string content)
        : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request);
            Bodies.Add(
                request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken)
            );

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            };
        }
    }
}
