using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Platform.Persistence.Postgres.Infrastructure.Extensions;

/// <summary>Регистрация синхронного прогона миграций для конкретного контекста.</summary>
public sealed class MarsSchemaStartupMarker(
    string schemaName,
    Action<ILogger> setLogger,
    Func<CancellationToken, Task> migrate
)
{
    public string SchemaName { get; } = schemaName;

    public void SetLogger(ILogger logger) => setLogger(logger);

    public Task MigrateAsync(CancellationToken cancellationToken) => migrate(cancellationToken);
}
