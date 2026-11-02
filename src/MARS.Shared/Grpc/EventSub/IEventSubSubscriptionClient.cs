namespace MARS.Shared.Grpc.EventSub;

/// <summary>
/// Клиент управления подписками Twitch EventSub в MARS.TwitchCore.
/// Внешние сервисы используют его вместо прямого доступа к Twitch API и токену.
/// </summary>
public interface IEventSubSubscriptionClient
{
    /// <summary>Создать подписку либо получить уже существующую.</summary>
    Task<CreateEventSubSubscriptionReply> CreateSubscriptionAsync(
        CreateEventSubSubscriptionRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Получить подписки Twitch EventSub, при необходимости отфильтровав по типу события.
    /// </summary>
    Task<ListEventSubSubscriptionsReply> ListSubscriptionsAsync(
        ListEventSubSubscriptionsRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Удалить подписку по идентификатору Twitch.</summary>
    Task<DeleteEventSubSubscriptionReply> DeleteSubscriptionAsync(
        DeleteEventSubSubscriptionRequest request,
        CancellationToken cancellationToken = default
    );
}
