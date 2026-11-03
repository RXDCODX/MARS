using System.Reflection;
using MARS.Shared.Messaging;
using MARS.TestKit;

namespace MARS.Shared.Tests.Messaging;

/// <summary>
/// Договор об именах RabbitMQ: в очереди видно сервис-владелец и назначение, в
/// routing key — сущность и действие.
/// </summary>
/// <remarks>
/// <para>
/// Без этой проверки имена снова разъедутся: очередь, названная по назначению
/// без владельца (<c>alerts.system</c>), не говорит, кто её читает, а ключ
/// <c>twitch.message.received</c> называет сервис вместо сущности. И то и другое
/// видно в журнале брокера, но только тому, кто уже знает ответ.
/// </para>
/// <para>
/// Проверяется форма, а не список значений: перечень имён уже записан в
/// <see cref="RabbitMqConfig"/>, и второй список в тесте разошёлся бы с ним
/// молча. Исключение — владелец очереди: он обязан быть настоящим проектом из
/// <c>src/</c>, иначе опечатка в имени сервиса останется незамеченной.
/// </para>
/// </remarks>
public class RabbitMqNamingTests
{
    private const string ExchangeName = "mars.events";

    [Fact]
    public void QueuesNameOwnerAndPurpose()
    {
        var queues = GroupMembers("Queues");

        Assert.NotEmpty(queues);
        Assert.All(
            queues,
            queue =>
                Assert.Matches("^mars\\.[a-z0-9]+(?:-[a-z0-9]+)*\\.[a-z0-9]+(?:-[a-z0-9]+)*$", queue.Value)
        );
        Assert.Equal(queues.Count, queues.Select(queue => queue.Value).Distinct().Count());
    }

    [Fact]
    public void QueueOwnerIsAProjectFromSourceTree()
    {
        var services = ServiceNames();

        foreach (var queue in GroupMembers("Queues"))
        {
            var owner = queue.Value.Split('.')[1];

            Assert.True(
                services.Contains(owner),
                $"Очередь {queue.Name} = \"{queue.Value}\": сервис \"{owner}\" не найден в src/*.csproj"
            );
        }
    }

    [Fact]
    public void EveryQueueIsReadSomewhere()
    {
        var sources = RepositoryFile.Sources("src").Select(File.ReadAllText).ToArray();

        foreach (var queue in GroupMembers("Queues"))
        {
            Assert.True(
                sources.Any(source => source.Contains($"Queues.{queue.Name}", StringComparison.Ordinal)),
                $"Очередь {queue.Name} = \"{queue.Value}\" никто не читает: в src/ нет ссылки Queues.{queue.Name}"
            );
        }
    }

    [Fact]
    public void RoutingKeysNameEntityAndAction()
    {
        var keys = GroupMembers("RoutingKeys");

        Assert.NotEmpty(keys);
        Assert.All(
            keys,
            key =>
                Assert.Matches("^[a-z0-9]+(?:-[a-z0-9]+)*(?:\\.[a-z0-9]+(?:-[a-z0-9]+)*)+$", key.Value)
        );
        Assert.Equal(keys.Count, keys.Select(key => key.Value).Distinct().Count());
    }

    [Fact]
    public void RoutingKeyDoesNotNameOwningService()
    {
        var services = ServiceNames();

        foreach (var key in GroupMembers("RoutingKeys"))
        {
            var entity = key.Value.Split('.')[0];

            Assert.False(
                services.Contains(entity),
                $"Ключ {key.Name} = \"{key.Value}\": \"{entity}\" — имя сервиса, а не сущности"
            );
        }
    }

    [Fact]
    public void QueueAndRoutingKeyNamesLiveInTheirGroups()
    {
        var stray = typeof(RabbitMqConfig)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => field.Name)
            .Where(name => name is not (nameof(RabbitMqConfig.SectionName) or nameof(RabbitMqConfig.ExchangeName)))
            .ToArray();

        Assert.Empty(stray);
    }

    [Fact]
    public void ExchangeIsSharedAndUnchanged()
    {
        Assert.Equal(ExchangeName, RabbitMqConfig.ExchangeName);
    }

    private static IReadOnlyList<Constant> GroupMembers(string groupName)
    {
        var group = typeof(RabbitMqConfig).GetNestedType(groupName, BindingFlags.Public);

        Assert.NotNull(group);

        return group.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => new Constant(field.Name, (string)field.GetRawConstantValue()!))
            .ToArray();
    }

    private sealed record Constant(string Name, string Value);

    private static HashSet<string> ServiceNames()
    {
        var solution = RepositoryFile.Find("MARS.slnx");
        var root = Path.GetDirectoryName(solution)!;

        return Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && name.StartsWith("MARS.", StringComparison.Ordinal))
            .Select(name => name!["MARS.".Length..].Replace('.', '-').ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
    }
}
