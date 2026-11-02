using Google.Protobuf;
using Google.Protobuf.Reflection;
using MARS.Shared.Grpc.EventSub;

namespace MARS.Shared.Tests.Grpc;

/// <summary>
/// Контракт создания подписок Twitch EventSub фиксируется тестом: набор полей и
/// их типы — это то, о чём договариваются вызывающий сервис и владелец
/// Twitch-подключения. Переименованное или выпавшее поле ломает разговор молча,
/// а всплывает на стенде, когда подписка перестала создаваться.
/// </summary>
/// <remarks>
/// Условия подписки передаются картой, а не отдельными полями: набор ключей
/// зависит от типа события (broadcaster_user_id, moderator_user_id,
/// to_broadcaster_user_id), и перечисление их полями отставало бы от каждого
/// нового типа EventSub. Тест держит именно это решение, а не конкретные ключи.
/// </remarks>
public class EventSubSubscriptionContractTests
{
    [Fact]
    public void Запрос_Создания_Несёт_Тип_Версию_Условия_И_Транспорт()
    {
        var descriptor = CreateEventSubSubscriptionRequest.Descriptor;

        Assert.Equal(
            ["type", "version", "condition", "transport", "requested_by"],
            FieldNames(descriptor)
        );

        var condition = descriptor.FindFieldByName("condition");
        var mapFields = condition.MessageType.Fields.InDeclarationOrder();
        Assert.True(condition.IsMap);
        Assert.Equal(FieldType.String, mapFields[0].FieldType);
        Assert.Equal(FieldType.String, mapFields[1].FieldType);
        Assert.Equal(
            "EventSubTransportInfo",
            descriptor.FindFieldByName("transport").MessageType.Name
        );
        Assert.Equal(FieldType.String, descriptor.FindFieldByName("requested_by").FieldType);
    }

    [Fact]
    public void Транспорт_Несёт_Способ_И_Идентификатор_Сессии()
    {
        var descriptor = EventSubTransportInfo.Descriptor;

        Assert.Equal(
            ["method", "session_id", "callback", "secret", "conduit_id"],
            FieldNames(descriptor)
        );
        Assert.Equal(
            "EventSubTransportMethodKind",
            descriptor.FindFieldByName("method").EnumType.Name
        );
        Assert.Equal(FieldType.String, descriptor.FindFieldByName("session_id").FieldType);
    }

    [Fact]
    public void Способы_Транспорта_Совпадают_с_Twitch()
    {
        Assert.Equal(
            [
                "EVENT_SUB_TRANSPORT_METHOD_KIND_UNSPECIFIED",
                "EVENT_SUB_TRANSPORT_METHOD_KIND_WEBSOCKET",
                "EVENT_SUB_TRANSPORT_METHOD_KIND_WEBHOOK",
                "EVENT_SUB_TRANSPORT_METHOD_KIND_CONDUIT",
            ],
            EventSubTransportInfo
                .Descriptor.FindFieldByName("method")
                .EnumType.Values.Select(value => value.Name)
        );
    }

    [Fact]
    public void Описание_Подписки_Несёт_Данные_Ответа_Twitch()
    {
        var descriptor = EventSubSubscriptionInfo.Descriptor;

        Assert.Equal(
            ["id", "status", "type", "version", "condition", "transport", "created_at", "cost"],
            FieldNames(descriptor)
        );
        Assert.True(descriptor.FindFieldByName("condition").IsMap);
        Assert.Equal(FieldType.Int64, descriptor.FindFieldByName("cost").FieldType);
        Assert.Equal(
            "EventSubTransportInfo",
            descriptor.FindFieldByName("transport").MessageType.Name
        );
    }

    [Fact]
    public void Ответ_Создания_Отличает_Новую_Подписку_От_Существующей()
    {
        var descriptor = CreateEventSubSubscriptionReply.Descriptor;

        Assert.Equal(["subscription", "created"], FieldNames(descriptor));
        Assert.Equal(FieldType.Bool, descriptor.FindFieldByName("created").FieldType);
        Assert.Equal(
            "EventSubSubscriptionInfo",
            descriptor.FindFieldByName("subscription").MessageType.Name
        );
    }

    [Fact]
    public void Список_Возвращает_Подписки_И_Расход_Лимита()
    {
        var descriptor = ListEventSubSubscriptionsReply.Descriptor;

        var subscriptions = descriptor.FindFieldByName("subscriptions");
        Assert.True(subscriptions.IsRepeated);
        Assert.Equal("EventSubSubscriptionInfo", subscriptions.MessageType.Name);
        Assert.Equal(FieldType.Int64, descriptor.FindFieldByName("total_cost").FieldType);
        Assert.Equal(FieldType.Int64, descriptor.FindFieldByName("max_total_cost").FieldType);
    }

    [Fact]
    public void Удаление_Адресуется_Идентификатором_Подписки()
    {
        Assert.Equal(["id"], FieldNames(DeleteEventSubSubscriptionRequest.Descriptor));
        Assert.Equal(["id"], FieldNames(DeleteEventSubSubscriptionReply.Descriptor));
    }

    [Fact]
    public void Запрос_Создания_Переживает_Сериализацию()
    {
        var request = new CreateEventSubSubscriptionRequest
        {
            Type = "channel.follow",
            Version = "2",
            RequestedBy = "MARS.Alerts",
            Transport = new EventSubTransportInfo
            {
                Method = EventSubTransportMethodKind.Websocket,
                SessionId = "session-1",
            },
        };
        request.Condition.Add("broadcaster_user_id", "12345");
        request.Condition.Add("moderator_user_id", "12345");

        var parsed = CreateEventSubSubscriptionRequest.Parser.ParseFrom(request.ToByteArray());

        Assert.Equal("channel.follow", parsed.Type);
        Assert.Equal("2", parsed.Version);
        Assert.Equal("MARS.Alerts", parsed.RequestedBy);
        Assert.Equal("session-1", parsed.Transport.SessionId);
        Assert.Equal(EventSubTransportMethodKind.Websocket, parsed.Transport.Method);
        Assert.Equal("12345", parsed.Condition["broadcaster_user_id"]);
        Assert.Equal("12345", parsed.Condition["moderator_user_id"]);
    }

    private static string[] FieldNames(MessageDescriptor descriptor)
    {
        return descriptor.Fields.InDeclarationOrder().Select(field => field.Name).ToArray();
    }
}
