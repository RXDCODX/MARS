using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
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

        lock (_sync)
        {
            switch (instrument)
            {
                case Counter<long>:
                case Counter<int>:
                case Counter<double>:
                case Counter<decimal>:
                    if (!_counters.TryGetValue(instrument.Name, out var counter))
                    {
                        counter = Metrics.CreateCounter(instrument.Name, GetHelp(instrument));
                        _counters[instrument.Name] = counter;
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
                    if (!_gauges.TryGetValue(instrument.Name, out var gauge))
                    {
                        gauge = Metrics.CreateGauge(instrument.Name, GetHelp(instrument));
                        _gauges[instrument.Name] = gauge;
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
                    if (!_histograms.TryGetValue(instrument.Name, out var histogram))
                    {
                        histogram = Metrics.CreateHistogram(instrument.Name, GetHelp(instrument));
                        _histograms[instrument.Name] = histogram;
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
