using System.Net;
using System.Text.Json;
using MARS.Shared.Exceptions;
using MARS.Shared.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Shared.Tests.Middleware;

/// <summary>
/// Промежуточная обработка ошибок: клиент обязан получить статус и JSON с
/// причиной, причём статус зависит от типа исключения. Иначе зритель видел бы
/// «500» вместо «сервис недоступен, попробуйте позже» и не понял бы, что
/// повторять попытку имеет смысл.
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task ServiceUnavailableIsReportedAsServiceUnavailable()
    {
        var context = await Invoke(_ => throw new ServiceUnavailableException("alerts"));

        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
        Assert.Contains("alerts", Body(context));
    }

    [Fact]
    public async Task ValidationErrorIsReportedAsBadRequest()
    {
        var context = await Invoke(_ => throw new ValidationException("поле не заполнено"));

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Contains("поле не заполнено", Body(context));
    }

    [Fact]
    public async Task MarsErrorIsReportedAsInternalError()
    {
        var context = await Invoke(_ => throw new MarsException("внутренняя ошибка MARS"));

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Contains("внутренняя ошибка MARS", Body(context));
    }

    /// <summary>
    /// Неизвестное исключение не выдаётся наружу текстом: в ответе может быть
    /// путь в файлах или строка подключения, поэтому наружу уходит только
    /// «Internal server error».
    /// </summary>
    [Fact]
    public async Task UnknownExceptionHidesDetails()
    {
        var context = await Invoke(_ => throw new InvalidOperationException("секретный путь"));

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        var body = Body(context);
        Assert.Contains("Internal server error", body);
        Assert.DoesNotContain("секретный путь", body);
    }

    [Fact]
    public async Task SuccessfulRequestIsPassedThrough()
    {
        var context = await Invoke(_ => Task.CompletedTask);

        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(string.Empty, Body(context));
    }

    [Fact]
    public async Task ResponseCarriesEnvelopeWithStatus()
    {
        var context = await Invoke(_ => throw new ValidationException("неверно"));

        var body = Body(context);
        Assert.Contains("\"success\":false", body);
        Assert.Contains("\"statusCode\":400", body);
        Assert.False(string.IsNullOrWhiteSpace(body));
    }

    private static async Task<HttpContext> Invoke(Func<HttpContext, Task> next)
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var middleware = new ExceptionHandlingMiddleware(
            _ => next(_),
            NullLogger<ExceptionHandlingMiddleware>.Instance
        );

        await middleware.InvokeAsync(context);

        return context;
    }

    private static string Body(HttpContext context)
    {
        context.Response.Body.Position = 0;

        return new StreamReader(context.Response.Body).ReadToEnd();
    }
}
