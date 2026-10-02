using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;
using Prometheus;

namespace MARS.Shared.Telemetry;

/// <summary>
/// Мост между метриками OpenTelemetry (<see cref="System.Diagnostics.Metrics"/>) и prometheus-net.
/// Без него счётчики из <see cref="MarsMetrics"/> не попадали бы в эндпоинт /metrics,
/// потому что сам prometheus-net ничего не знает про OTel Meter.
/// </summary>
public sealed class OpenTelemetryPrometheusBridge : IDisposable
{
    private const string MeterNamePrefix = "MARS.";

    private readonly MeterListener _listener;
    private readonly Dictionary<string, Counter> _counters = [];
    private readonly Dictionary<string, Gauge> _gauges = [];
    private readonly Dictionary<string, Histogram> _histograms = [];
    private readonly object _sync = new();

    public OpenTelemetryPrometheusBridge()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name.StartsWith(MeterNamePrefix, StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };

        _listener.SetMeasurementEventCallback<long>(
            (instrument, value, tags, _) => Record(instrument, value, tags)
        );
        _listener.SetMeasurementEventCallback<double>(
            (instrument, value, tags, _) => Record(instrument, value, tags)
        );
        _listener.SetMeasurementEventCallback<int>(
            (instrument, value, tags, _) => Record(instrument, (long)value, tags)
        );
        _listener.SetMeasurementEventCallback<decimal>(
            (instrument, value, tags, _) => Record(instrument, (double)value, tags)
        );

        _listener.Start();
    }

    private void Record(
        Instrument instrument,
        double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags
    )
    {
        var labelValues = ExtractLabelValues(tags);
        var metricName = ToPrometheusName(instrument.Name);
        var labelNames = ExtractLabelNames(instrument);

        lock (_sync)
        {
            switch (instrument)
            {
                case Counter<long>:
                case Counter<int>:
                case Counter<double>:
                case Counter<decimal>:
                    if (!_counters.TryGetValue(metricName, out var counter))
                    {
                        counter = Metrics.CreateCounter(
                            metricName,
                            GetHelp(instrument),
                            labelNames
                        );
                        _counters[metricName] = counter;
                    }

                    if (labelValues.Length == 0)
                    {
                        counter.Inc(measurement);
                    }
                    else
                    {
                        counter.WithLabels(labelValues).Inc(measurement);
                    }

                    break;

                case UpDownCounter<long>:
                case UpDownCounter<int>:
                case UpDownCounter<double>:
                case UpDownCounter<decimal>:
                    if (!_gauges.TryGetValue(metricName, out var gauge))
                    {
                        gauge = Metrics.CreateGauge(metricName, GetHelp(instrument), labelNames);
                        _gauges[metricName] = gauge;
                    }

                    if (labelValues.Length == 0)
                    {
                        gauge.Set(measurement);
                    }
                    else
                    {
                        gauge.WithLabels(labelValues).Set(measurement);
                    }

                    break;

                case Histogram<long>:
                case Histogram<int>:
                case Histogram<double>:
                case Histogram<decimal>:
                    if (!_histograms.TryGetValue(metricName, out var histogram))
                    {
                        histogram = Metrics.CreateHistogram(
                            metricName,
                            GetHelp(instrument),
                            labelNames
                        );
                        _histograms[metricName] = histogram;
                    }

                    if (labelValues.Length == 0)
                    {
                        histogram.Observe(measurement);
                    }
                    else
                    {
                        histogram.WithLabels(labelValues).Observe(measurement);
                    }

                    break;

                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Имена меток берутся из объявления инструмента, а не из измерения.
    ///
    /// Prometheus создаёт метрику по списку имён меток, и <c>WithLabels</c>
    /// обязан передать ровно столько же значений. Инструмент без объявленных
    /// меток, измеренный с меткой, ронял мост ArgumentException — и это
    /// исключение уходило в вызывающий код.
    /// </summary>
    private static string[] ExtractLabelNames(Instrument instrument)
    {
        var names = new List<string>();

        if (instrument.Tags is not null)
        {
            foreach (var tag in instrument.Tags)
            {
                names.Add(tag.Key);
            }
        }

        return [.. names];
    }

    /// <summary>
    /// Приводит имя инструмента OTel к допустимому для Prometheus.
    /// Имена в <see cref="MarsMetrics"/> содержат точки («mars.twitch.rewards.redeemed»),
    /// а prometheus-net валидирует имя регулярным выражением
    /// ^[a-zA-Z_][a-zA-Z0-9_]*$ и бросает ArgumentException. Исключение прилетало
    /// в вызывающий код: каждая публикация и каждое чтение из RabbitMQ роняли
    /// счётчик, а в <c>RabbitMqConsumerBase</c> падение после успешного ack
    /// попадало в catch обработки сообщения и засчитывалось как ошибка.
    /// </summary>
    internal static string ToPrometheusName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length);

        for (var i = 0; i < name.Length; i++)
        {
            var symbol = name[i];

            var isAllowed =
                (symbol >= 'a' && symbol <= 'z')
                || (symbol >= 'A' && symbol <= 'Z')
                || (symbol >= '0' && symbol <= '9')
                || symbol == '_';

            if (i == 0 && symbol >= '0' && symbol <= '9')
            {
                isAllowed = false;
            }

            builder.Append(isAllowed ? symbol : '_');
        }

        return builder.ToString();
    }

    private static string GetHelp(Instrument instrument)
    {
        return string.IsNullOrWhiteSpace(instrument.Description)
            ? instrument.Name
            : instrument.Description;
    }

    private static string[] ExtractLabelValues(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.Length == 0)
        {
            return [];
        }

        var values = new string[tags.Length];

        for (var i = 0; i < tags.Length; i++)
        {
            values[i] = tags[i].Value?.ToString() ?? string.Empty;
        }

        return values;
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
