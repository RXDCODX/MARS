using System.Text.Json;
using MARS.Telegram.Controllers;
using MARS.Telegram.Entities;
using MARS.Telegram.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WTelegram;

namespace MARS.Telegram.Tests.Controllers;

/// <summary>
/// Приём кода подтверждения входа: странице важно отличить «код не подошёл» от
/// «сервис сломался».
/// </summary>
/// <remarks>
/// Раньше действие возвращало 500 на любую ошибку входа, и страница показывала
/// «ошибку сервера» там, где виноват был введённый код.
/// </remarks>
public class WTelegramControllerTests
{
    /// <summary>
    /// Отказ Telegram на сам код — это ответ на ввод, а не поломка службы: клиент
    /// показывает сообщение и ждёт нового кода.
    /// </summary>
    [Fact]
    public async Task Неверный_Код_Даёт_Отказ_С_Причиной()
    {
        var controller = Create(reloginError: new WTException("400 PHONE_CODE_INVALID"));

        var result = await SubmitAsync(controller, "00000");

        var failure = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, failure.StatusCode);
        Assert.Contains("PHONE_CODE_INVALID", Message(failure));
    }

    /// <summary>
    /// Ошибка входа, кодом не вызванная, остаётся ошибкой службы: придумывать
    /// пользователю «неверный код» там, где недоступен прокси, незачем.
    /// </summary>
    [Fact]
    public async Task Прочая_Ошибка_Входа_Даёт_Ошибку_Службы()
    {
        var controller = Create(reloginError: new InvalidOperationException("прокси недоступен"));

        var result = await SubmitAsync(controller, "54321");

        var failure = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, failure.StatusCode);
    }

    /// <summary>
    /// Пустой код не принимается: клиент отверг бы его так же, а страница показала
    /// бы «принято» и замолчала.
    /// </summary>
    [Fact]
    public async Task Пустой_Код_Отклоняется_Без_Попытки_Входа()
    {
        var service = new Mock<IWTelegramClientService>();
        var controller = new WTelegramController(
            service.Object,
            NullLogger<WTelegramController>.Instance
        );

        var result = await SubmitAsync(controller, "   ");

        var failure = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, failure.StatusCode);
        service.Verify(item => item.ReLoginAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static WTelegramController Create(Exception? reloginError)
    {
        var service = new Mock<IWTelegramClientService>();
        service.Setup(item => item.SubmitVerificationCode(It.IsAny<string>())).Returns(true);
        service
            .Setup(item => item.GetClientStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WTelegramClientStatus());
        service
            .Setup(item => item.ReLoginAsync(It.IsAny<CancellationToken>()))
            .Returns(reloginError is null ? Task.CompletedTask : Task.FromException(reloginError));

        return new WTelegramController(service.Object, NullLogger<WTelegramController>.Instance);
    }

    private static Task<IActionResult> SubmitAsync(WTelegramController controller, string code) =>
        controller.SubmitVerificationCode(
            new VerificationCodeRequest { Code = code },
            TestContext.Current.CancellationToken
        );

    /// <summary>
    /// Текст ответа достаётся из тела результата: страница показывает именно его,
    /// а HTTP-код читает как «ошибка» без деталей.
    /// </summary>
    private static string Message(ObjectResult result) => JsonSerializer.Serialize(result.Value);
}
