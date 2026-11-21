using System.Reflection;
using MARS.Shared.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Platform.Infrastructure.Events;

/// <summary>
/// Отправляет активную ветку сообщения-«oneof» в метод клиента SignalR.
/// </summary>
/// <remarks>
/// Имя ветки задаёт метод, сигнатура метода — наличие аргумента. Исключения
/// (например, <c>StateUpdated</c> → <c>ReceiveState</c>) и преобразование
/// полезной нагрузки задаются вызывающим реле точечно, а не общим switch.
/// Ветки, для которых метода в контракте нет, не отправляются: это явное
/// решение реле, а не забытая ветка.
/// </remarks>
public static class OneofHubRelay
{
    public static async Task DispatchAsync<TEvent, TClient>(
        TEvent notification,
        IHubClients clients,
        IReadOnlyDictionary<string, string>? methodNames,
        Func<string, object?, object?>? payloadTransform,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var active = OneofEventContract.GetActiveBranch(notification!);
        if (active is null)
        {
            return;
        }

        var (branch, payload) = active.Value;
        var methodName =
            methodNames is not null && methodNames.TryGetValue(branch, out var mapped)
                ? mapped
                : branch;

        var method = typeof(TClient).GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Instance
        );
        if (method is null)
        {
            return;
        }

        var value = payloadTransform?.Invoke(branch, payload) ?? payload;
        object?[] arguments = method.GetParameters().Length == 0 ? [] : [value];
        await clients.All.SendCoreAsync(methodName, arguments, cancellationToken);
    }
}
