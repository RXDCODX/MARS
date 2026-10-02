using System.Diagnostics.Metrics;
using System.IO;
using System.Text;
using MARS.Shared.Telemetry;
using Prometheus;

namespace MARS.Shared.Tests.Telemetry;

public class OpenTelemetryPrometheusBridgeTests
{
    [Theory]
    [InlineData("mars.twitch.rewards.redeemed", "mars_twitch_rewards_redeemed")]
    [InlineData("mars.commands.latency_ms", "mars_commands_latency_ms")]
    [InlineData("mars.rabbitmq.consumer_errors", "mars_rabbitmq_consumer_errors")]
    [InlineData("already_valid", "already_valid")]
    [InlineData("Mixed.Case-Name", "Mixed_Case_Name")]
    [InlineData("1st.metric", "_st_metric")]
    [InlineData("2024.value", "_024_value")]
    public void ToPrometheusName_ProducesNamePrometheusNetAccepts(string input, string expected)
    {
        // Регулярное выражение prometheus-net: ^[a-zA-Z_][a-zA-Z0-9_]*$
        var actual = OpenTelemetryPrometheusBridge.ToPrometheusName(input);

        Assert.Equal(expected, actual);
        Assert.Matches("^[a-zA-Z_][a-zA-Z0-9_]*$", actual);
    }

    [Fact]
    public void ToPrometheusName_AcceptsEveryInstrumentNameFromMarsMetrics()
    {
        var meter = new Meter("MARS.NameCheck", "1.0.0");

        try
        {
            var counter = meter.CreateCounter<long>("seed");
            var histogram = meter.CreateHistogram<double>("seed_hist");

            // Имена инструментов в MarsMetrics содержат точки. Проверяем, что
            // нормализация даёт валидное имя для каждого из них.
            var names = new[]
            {
                "mars.interservice.calls",
                "mars.interservice.latency_ms",
                "mars.twitch.events.received",
                "mars.twitch.rewards.redeemed",
                "mars.twitch.rewards.processed",
                "mars.twitch.rewards.failed",
                "mars.twitch.event.processing_ms",
                "mars.media.tracks.played",
                "mars.media.tts.processed",
                "mars.alerts.sent",
                "mars.alerts.by_type",
                "mars.rabbitmq.published",
                "mars.rabbitmq.consumed",
                "mars.rabbitmq.consumer_errors",
                "mars.rabbitmq.unhandled_messages",
                "mars.commands.executed",
                "mars.commands.by_platform",
                "mars.commands.by_name",
                "mars.commands.by_target",
                "mars.commands.succeeded",
                "mars.commands.failed",
                "mars.commands.latency_ms",
                "mars.commands.unknown",
            };

            foreach (var name in names)
            {
                Assert.Matches(
                    "^[a-zA-Z_][a-zA-Z0-9_]*$",
                    OpenTelemetryPrometheusBridge.ToPrometheusName(name)
                );
            }

            // Sanity: сам meter тоже должен принимать точечные имена (в этом суть теста).
            counter.Add(1);
            histogram.Record(1);
        }
        finally
        {
            meter.Dispose();
        }
    }

    [Fact]
    public void Record_DoesNotThrowForDottedInstrumentName()
    {
        // Регрессия на живой баг: prometheus-net бросал ArgumentException на точках
        // в имени, и исключение уходило в вызывающий код — счётчики RabbitMQ
        // роняли каждую публикацию и каждое чтение.
        using var bridge = new OpenTelemetryPrometheusBridge();
        using var meter = new Meter("MARS.BridgeThrow", "1.0.0");

        var counter = meter.CreateCounter<long>("mars.bridge.counter", description: "dotted");
        var histogram = meter.CreateHistogram<double>("mars.bridge.latency_ms", unit: "ms");

        var exception = Record.Exception(() =>
        {
            counter.Add(1);
            histogram.Record(2.5);
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task Record_ExposesSanitizedNameOnDefaultRegistry()
    {
        using var bridge = new OpenTelemetryPrometheusBridge();
        using var meter = new Meter("MARS.BridgeRegistry", "1.0.0");

        var counter = meter.CreateCounter<long>(
            "mars.bridge.registry.counter",
            description: "dotted"
        );
        counter.Add(7);

        using var stream = new MemoryStream();
        await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(
            stream,
            TestContext.Current.CancellationToken
        );

        var exported = Encoding.UTF8.GetString(stream.ToArray());

        Assert.Contains("mars_bridge_registry_counter", exported);
    }

    /// <summary>
    /// Метка измерения обязана попадать в Prometheus: счётчики очередей
    /// различаются по имени очереди, и без метки они слились бы в одно число.
    ///
    /// Регрессия на живой баг: инструмент объявлялся без меток, а вызов передавал
    /// <c>KeyValuePair("queue", …)</c>. prometheus-net бросал ArgumentException
    /// прямо внутри <c>Add</c>, и исключение уходило в
    /// <c>RabbitMqConsumerBase.HandleDeliveryAsync</c> — то есть УСПЕШНО
    /// обработанное сообщение уходило в повтор, а потом в dead-letter.
    /// </summary>
    [Fact]
    public async Task Record_KeepsDeclaredLabels()
    {
        using var bridge = new OpenTelemetryPrometheusBridge();
        using var meter = new Meter("MARS.BridgeLabels", "1.0.0");

        var counter = meter.CreateCounter<long>(
            "mars.bridge.labels.counter",
            description: "tagged",
            unit: null,
            tags: [new KeyValuePair<string, object?>("queue", string.Empty)]
        );

        var exception = Record.Exception(() =>
            counter.Add(3, new KeyValuePair<string, object?>("queue", "orders"))
        );

        Assert.Null(exception);

        using var stream = new MemoryStream();
        await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(
            stream,
            TestContext.Current.CancellationToken
        );

        var exported = Encoding.UTF8.GetString(stream.ToArray());

        Assert.Contains("queue=\"orders\"", exported);
    }

    /// <summary>
    /// Объявленные метки обязаны совпадать с теми, что передают вызовы в
    /// <c>MarsMetrics</c>: иначе мост создаёт метрику без меток, а измерение
    /// приходит с меткой — ровно тот случай, что ронял каждое сообщение.
    /// </summary>
    [Fact]
    public async Task RabbitMqMetricsAcceptTheTagsTheirCallSitesPass()
    {
        using var bridge = new OpenTelemetryPrometheusBridge();
        var exception = Record.Exception(() =>
        {
            MarsMetrics.RabbitMqPublished.Add(
                1,
                new KeyValuePair<string, object?>("exchange", "mars.events"),
                new KeyValuePair<string, object?>("routing_key", "twitch.chat.send")
            );
            MarsMetrics.RabbitMqConsumed.Add(
                1,
                new KeyValuePair<string, object?>("queue", "orders")
            );
            MarsMetrics.RabbitMqConsumerErrors.Add(
                1,
                new KeyValuePair<string, object?>("queue", "orders")
            );
            MarsMetrics.RabbitMqUnhandledMessages.Add(
                1,
                new KeyValuePair<string, object?>("routing_key", "unknown.event")
            );
        });

        Assert.Null(exception);

        using var stream = new MemoryStream();
        await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(
            stream,
            TestContext.Current.CancellationToken
        );

        var exported = Encoding.UTF8.GetString(stream.ToArray());

        Assert.Contains("mars_rabbitmq_consumed", exported);
        Assert.Contains("queue=\"orders\"", exported);
    }
}
