using System.Text.RegularExpressions;
using MARS.Alerts.Api.Controllers;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.DependencyInjection;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Shared.Contracts.Alerts;
using MARS.TestKit;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MARS.Alerts.Tests;

/// <summary>
/// Гарантирует, что внутренний эндпоинт определений наград отдаёт цвета
/// в формате #RRGGBB — иначе Twitch получит пустой/null цвет и поставит
/// случайный (см. регрессию, из-за которой цвета не применялись).
/// </summary>
public class InternalRewardsControllerTests
{
    private static IReadOnlyList<IRewardAlertHandler> RealHandlers() =>
        [
            .. RewardHandlerRegistration
                .DiscoverHandlerTypes()
                .Select(type => (IRewardAlertHandler)Stub.Resolve(type, depth: 3)!),
        ];

    [Fact]
    public async Task GetDefinitions_ReturnsNonEmptyListWithValidHexColors()
    {
        var controller = new InternalRewardsController(
            RealHandlers(),
            NullLogger<InternalRewardsController>.Instance
        );

        var action = await controller.GetDefinitions(CancellationToken.None);
        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(action.Result);
        var payload = Assert.IsType<OperationResult<List<RewardDefinitionSnapshot>>>(ok.Value);

        Assert.True(payload.Success, payload.ErrorMessage);
        Assert.NotEmpty(payload.Result!);

        var colorPattern = new Regex("^#[0-9A-Fa-f]{6}$");
        foreach (var def in payload.Result!)
        {
            Assert.Matches(colorPattern, def.BackgroundColor);
        }
    }
}
