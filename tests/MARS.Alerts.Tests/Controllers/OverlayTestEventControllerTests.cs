using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MARS.Alerts.Api.Controllers;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MARS.Alerts.Tests.Controllers;

public class OverlayTestEventControllerTests
{
    private readonly Mock<IAlertsHub> _clients = new();
    private readonly Mock<IHubClients<IAlertsHub>> _hubClients = new();
    private readonly List<(string Method, object?[] Args)> _sent = [];

    public OverlayTestEventControllerTests()
    {
        _clients
            .As<IClientProxy>()
            .Setup(p =>
                p.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, object?[], CancellationToken>((m, a, _) => _sent.Add((m, a)))
            .Returns(Task.CompletedTask);
        _hubClients.Setup(c => c.All).Returns(_clients.Object);
    }

    [Fact]
    public async Task Post_WhenDisabled_ReturnsFail()
    {
        var controller = CreateController(enabled: false);

        var result = await controller.Post(
            new OverlayTestEventRequest { EventName = "Alert", PayloadJson = "{}" },
            TestContext.Current.CancellationToken
        );

        var op = Assert.IsType<OkObjectResult>(result.Result).Value as OperationResult<string>;
        Assert.NotNull(op);
        Assert.False(op!.Success);
        Assert.Contains("OverlayDevTools", op.ErrorMessage ?? string.Empty);
        Assert.Empty(_sent);
    }

    [Fact]
    public async Task Post_UnknownEvent_ReturnsFail()
    {
        var controller = CreateController(enabled: true);

        var result = await controller.Post(
            new OverlayTestEventRequest { EventName = "NoSuchEvent", PayloadJson = "{}" },
            TestContext.Current.CancellationToken
        );

        var op = Assert.IsType<OkObjectResult>(result.Result).Value as OperationResult<string>;
        Assert.NotNull(op);
        Assert.False(op!.Success);
        Assert.Empty(_sent);
    }

    [Fact]
    public async Task Post_Alert_SendsRawPayload()
    {
        var controller = CreateController(enabled: true);
        var payload = """{"media":{"id":"11111111-2222-3333-4444-555555555555"}}""";

        var result = await controller.Post(
            new OverlayTestEventRequest { EventName = "Alert", PayloadJson = payload },
            TestContext.Current.CancellationToken
        );

        var op = Assert.IsType<OkObjectResult>(result.Result).Value as OperationResult<string>;
        Assert.NotNull(op);
        Assert.True(op!.Success, op.ErrorMessage);
        var sent = Assert.Single(_sent);
        Assert.Equal("Alert", sent.Method);
        var arg = Assert.IsType<JsonElement>(sent.Args[0]);
        Assert.Equal(JsonValueKind.Object, arg.ValueKind);
    }

    [Fact]
    public async Task Post_Credits_SendsParameterlessEvent()
    {
        var controller = CreateController(enabled: true);

        var result = await controller.Post(
            new OverlayTestEventRequest { EventName = "Credits", PayloadJson = null },
            TestContext.Current.CancellationToken
        );

        var op = Assert.IsType<OkObjectResult>(result.Result).Value as OperationResult<string>;
        Assert.True(op!.Success);
        var sent = Assert.Single(_sent);
        Assert.Equal("Credits", sent.Method);
        Assert.Empty(sent.Args);
    }

    [Fact]
    public async Task Post_InvalidJson_ReturnsFail()
    {
        var controller = CreateController(enabled: true);

        var result = await controller.Post(
            new OverlayTestEventRequest { EventName = "Alert", PayloadJson = "{broken" },
            TestContext.Current.CancellationToken
        );

        var op = Assert.IsType<OkObjectResult>(result.Result).Value as OperationResult<string>;
        Assert.NotNull(op);
        Assert.False(op!.Success);
        Assert.Empty(_sent);
    }

    [Fact]
    public void SupportedEvents_MatchManifest()
    {
        var manifestPath = FindRepoFile(
            Path.Combine(
                "src",
                "MARS.Alerts",
                "MARS.Alerts.Presentation.Overlay",
                "Hubs",
                "overlay-hub.manifest.json"
            )
        );
        var manifest = JsonSerializer.Deserialize<string[]>(File.ReadAllText(manifestPath));
        Assert.NotNull(manifest);

        var supported = OverlayTestEventController.SupportedEvents.OrderBy(x => x).ToArray();
        var expected = manifest!.OrderBy(x => x).ToArray();
        Assert.Equal(expected, supported);
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"Не найден файл {relativePath} вверх от {AppContext.BaseDirectory}"
        );
    }

    private OverlayTestEventController CreateController(bool enabled)
    {
        var hubContext = new Mock<IHubContext<AlertsHub, IAlertsHub>>();
        hubContext.SetupGet(h => h.Clients).Returns(_hubClients.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OverlayDevTools:Enabled"] = enabled ? "true" : "false",
                }
            )
            .Build();

        return new OverlayTestEventController(
            hubContext.Object,
            config,
            NullLogger<OverlayTestEventController>.Instance
        );
    }
}
