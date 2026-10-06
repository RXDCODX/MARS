using MARS.Shared.Models;
using MARS.SoundRequest.Controllers;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Tests.Grpc;
using MARS.SoundRequest.Tests.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.SoundRequest.Tests.Controllers;

/// <summary>
/// Контракт эндпоинта статуса Spotify.
/// </summary>
/// <remarks>
/// Статус — это ответ на вопрос, а не операция. «Spotify не подключён» не
/// ошибка, а нормальное состояние стенда без учётных данных, и возвращать его
/// отказом нельзя: транспорт клиента превращает отказ в исключение, а панель
/// админа писала в консоль <c>console.error</c>. На стенде без ключей Spotify
/// это давало единственный красный тест в e2e — маршрут <c>/spotify</c>
/// проверяет, что страница не дала ошибок в консоли.
/// </remarks>
public class SpotifyAuthStatusTests
{
    /// <summary>
    /// Без подключения эндпоинт отвечает успехом и данными, а не отказом.
    /// </summary>
    [Fact]
    public async Task СтатусБезПодключенияУспешен()
    {
        var controller = new SpotifyAuthController(await CreateServiceAsync());

        var action = await controller.GetStatus(TestContext.Current.CancellationToken);
        var result = Unwrap(action);

        Assert.True(result.Success, $"статус без подключения вернул отказ: {result.ErrorMessage}");
        Assert.NotNull(result.Result);
        Assert.False(result.Result!.IsLinked);
        Assert.Contains(
            "не подключен",
            result.Result!.Message ?? string.Empty,
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static OperationResult<SpotifyAuthStatusResult> Unwrap(
        ActionResult<OperationResult<SpotifyAuthStatusResult>> action
    )
    {
        // Контроллер возвращает сам OperationResult, а не IActionResult: при
        // прямом вызове метода он оказывается в Value, а не в Result.
        if (action.Result is null)
        {
            return Assert.IsType<OperationResult<SpotifyAuthStatusResult>>(action.Value);
        }

        var ok = Assert.IsType<OkObjectResult>(action.Result);

        return Assert.IsType<OperationResult<SpotifyAuthStatusResult>>(ok.Value);
    }

    /// <summary>
    /// Сервис с пустым хранилищем: учётных данных нет, значит и связи нет.
    /// </summary>
    private static async Task<SpotifyAuthService> CreateServiceAsync()
    {
        var factory = new TestDbContextFactory();
        await using var context = await factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(instance => instance.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(new SpotifyStubHandler()));

        return new SpotifyAuthService(
            factory,
            new ConfigurationBuilder().Build(),
            httpClientFactory.Object,
            NullLogger<SpotifyAuthService>.Instance
        );
    }
}
