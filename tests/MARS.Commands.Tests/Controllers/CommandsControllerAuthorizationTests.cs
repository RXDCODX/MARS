using System.Net;
using System.Net.Http.Json;
using MARS.Commands.Controllers;
using MARS.Commands.Services;
using MARS.Commands.Services.Adapters;
using MARS.Shared.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MARS.Commands.Tests.Controllers;

/// <summary>
/// Проверяет, что исполнение команды закрыто ключом межсервисной аутентификации.
///
/// До защиты <c>POST /api/Commands/{name}/execute</c> был открыт наружу через
/// маршрут YARP, а <c>ApiCommandService.IsAdmin</c> возвращал константу
/// <c>true</c> — то есть любой, кто достал до шлюза, запускал админ-команды,
/// включая <c>shutdown</c> и <c>setenv</c>.
/// </summary>
public sealed class CommandsControllerAuthorizationTests
{
    private const string ApiKeyHeader = "X-Api-Key";
    private const string KnownApiKey = "test-service-key";

    [Fact]
    public async Task ExecuteWithoutApiKeyIsRejected()
    {
        var client = await StartHostAsync();

        var response = await client.PostAsync(
            "/api/Commands/srlist/execute",
            JsonContent.Create("x"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExecuteWithWrongApiKeyIsRejected()
    {
        var client = await StartHostAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Commands/srlist/execute")
        {
            Content = JsonContent.Create("x"),
        };
        request.Headers.Add(ApiKeyHeader, "wrong-key");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CommandListingStaysOpenForDiscovery()
    {
        var client = await StartHostAsync();

        var response = await client.GetAsync("/api/Commands/user", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Поднимает хост с реальными регистрациями сервиса: иначе тест проверял бы
    /// набор атрибутов, а не поведение схемы аутентификации.
    /// </summary>
    private static async Task<HttpClient> StartHostAsync()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ServiceAuth:ApiKey"] = KnownApiKey,
                ["RabbitMq:Host"] = string.Empty,
            }
        );

        builder.WebHost.UseTestServer();
        builder.Services.AddControllers().AddApplicationPart(typeof(CommandsController).Assembly);
        builder.Services.AddMarsAuthentication(builder.Configuration);

        // Реальные регистрации из Program.cs: подменять их моками нельзя,
        // тест проверяет связку «маршрут → политика → адаптер», а не атрибут.
        builder.Services.AddSingleton<CommandFactory>();
        builder.Services.AddSingleton<ICommandService, CommandExecutorService>();
        builder.Services.AddSingleton<ApiCommandService>();

        var app = builder.Build();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await app.StartAsync(TestContext.Current.CancellationToken);

        return app.GetTestServer().CreateClient();
    }
}