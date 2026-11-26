using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TwitchLib.Client.Interfaces;

namespace MARS.Twitch.Infrastructure.Services.Connection;

/// <summary>
/// Стабильная обёртка над <see cref="ITwitchClient"/>. Внешние потребители
/// получают один и тот же экземпляр на всё время жизни процесса, а
/// <see cref="TwitchConnectionManager"/> может подменить внутренний клиент при
/// пересоздании транспорта, не ломая подписки на события.
/// </summary>
public class TwitchClientProxy : DispatchProxy
{
    private volatile ITwitchClient? _inner;
    private readonly object _swapLock = new();
    private readonly Dictionary<string, List<Delegate>> _eventHandlers = new(
        StringComparer.Ordinal
    );

    public static ITwitchClient Create() =>
        DispatchProxy.Create<ITwitchClient, TwitchClientProxy>();

    public static TwitchClientProxy GetProxyInstance(ITwitchClient proxy) =>
        (TwitchClientProxy)(object)proxy;

    public void ReplaceClient(ITwitchClient newClient)
    {
        ArgumentNullException.ThrowIfNull(newClient);

        lock (_swapLock)
        {
            var oldClient = _inner;

            foreach (var pair in _eventHandlers)
            {
                var eventInfo = oldClient
                    ?.GetType()
                    .GetEvent(pair.Key, BindingFlags.Public | BindingFlags.Instance);
                foreach (var handler in pair.Value)
                {
                    eventInfo?.RemoveEventHandler(oldClient, handler);
                }
            }

            _inner = newClient;

            foreach (var pair in _eventHandlers)
            {
                var eventInfo = newClient
                    .GetType()
                    .GetEvent(pair.Key, BindingFlags.Public | BindingFlags.Instance);
                foreach (var handler in pair.Value)
                {
                    eventInfo?.AddEventHandler(newClient, handler);
                }
            }
        }
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
        {
            throw new ArgumentNullException(nameof(targetMethod));
        }

        var name = targetMethod.Name;

        if (name.StartsWith("add_", StringComparison.Ordinal))
        {
            var eventName = name["add_".Length..];
            if (args is null || args.Length == 0 || args[0] is not Delegate handler)
            {
                throw new ArgumentException("Event handler is required.", nameof(args));
            }

            lock (_swapLock)
            {
                if (!_eventHandlers.TryGetValue(eventName, out var handlers))
                {
                    handlers = new List<Delegate>();
                    _eventHandlers[eventName] = handlers;
                }

                handlers.Add(handler);

                var current = _inner;
                var eventInfo = current
                    ?.GetType()
                    .GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
                eventInfo?.AddEventHandler(current, handler);
            }

            return null;
        }

        if (name.StartsWith("remove_", StringComparison.Ordinal))
        {
            var eventName = name["remove_".Length..];
            var handler = args is { Length: > 0 } ? args[0] as Delegate : null;

            lock (_swapLock)
            {
                if (handler is not null && _eventHandlers.TryGetValue(eventName, out var handlers))
                {
                    handlers.Remove(handler);
                }

                var current = _inner;
                var eventInfo = current
                    ?.GetType()
                    .GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
                if (handler is not null)
                {
                    eventInfo?.RemoveEventHandler(current, handler);
                }
            }

            return null;
        }

        if (name.StartsWith("get_", StringComparison.Ordinal))
        {
            var inner = _inner;
            if (inner is null)
            {
                throw new InvalidOperationException("Twitch client is not initialized.");
            }

            var propertyName = name["get_".Length..];
            var property = inner
                .GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(inner);
        }

        if (name.StartsWith("set_", StringComparison.Ordinal))
        {
            var inner = _inner;
            if (inner is null)
            {
                throw new InvalidOperationException("Twitch client is not initialized.");
            }

            var propertyName = name["set_".Length..];
            var property = inner
                .GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            property?.SetValue(inner, args is { Length: > 0 } ? args[0] : null);
            return null;
        }

        var target = _inner;
        if (target is null)
        {
            throw new InvalidOperationException("Twitch client is not initialized.");
        }

        try
        {
            return targetMethod.Invoke(target, args);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }
}
