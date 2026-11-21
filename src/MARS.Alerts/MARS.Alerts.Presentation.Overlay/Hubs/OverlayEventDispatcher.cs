using System.Reflection;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Shared.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Presentation.Overlay.Hubs;

/// <summary>
/// Раскладывает <see cref="TelegramusEvent"/> по методам типизированного
/// клиента хаба.
/// </summary>
/// <remarks>
/// Имя метода и наличие аргумента выводятся из рефлексии: свойство сообщения
/// задаёт имя, а сигнатура метода <see cref="IAlertsHub"/> — форму вызова.
/// Вызов идёт через тот же типизированный прокси, что и раньше, поэтому строки
/// на проводе не меняются; ручной switch на 36 веток больше не нужен.
/// </remarks>
public static class OverlayEventDispatcher
{
    private static readonly IReadOnlyDictionary<string, MethodInfo> HubMethods = typeof(IAlertsHub)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .ToDictionary(method => method.Name, StringComparer.Ordinal);

    public static async Task DispatchAsync(
        IHubContext<AlertsHub, IAlertsHub> hubContext,
        TelegramusEvent notification,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var active = TelegramusEventContract.GetActiveBranch(notification);
        if (active is null)
        {
            return;
        }

        if (!HubMethods.TryGetValue(active.Value.Name, out var method))
        {
            throw new InvalidOperationException(
                $"У события {active.Value.Name} нет метода в контракте оверлейного хаба."
            );
        }

        object?[]? arguments = method.GetParameters().Length == 0 ? null : [active.Value.Payload];
        await (Task)method.Invoke(hubContext.Clients.All, arguments)!;
    }
}
