using Microsoft.Extensions.Configuration;
using Serilog;

namespace MARS.Shared.Logging;

public static class SerilogExtensions
{
    public static LoggerConfiguration AddMarsLogging(
        this LoggerConfiguration loggerConfig,
        IConfiguration configuration,
        string serviceName
    )
    {
        loggerConfig
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.FromLogContext()
            .Enrich.With<ActivityEnricher>()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj}{NewLine}{Exception}"
            );

        var seqUrl = configuration["Seq:Url"];
        if (!string.IsNullOrEmpty(seqUrl))
        {
            loggerConfig.WriteTo.Seq(seqUrl);
        }

        var otlpEndpoint = configuration["Otlp:Endpoint"];
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            loggerConfig.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpEndpoint;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                };
            });
        }

        return loggerConfig;
    }
}
