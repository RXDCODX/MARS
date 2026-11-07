using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Platform.Persistence.Postgres.Infrastructure.Extensions;

/// <inheritdoc cref="IMarsSchemaReady{TContext}"/>
public sealed class MarsSchemaReadySignal<TContext>(MarsSchemaMigrator<TContext> migrator)
    : IMarsSchemaReady<TContext>
    where TContext : DbContext
{
    public Task WaitAsync(CancellationToken cancellationToken) =>
        migrator.MigrateAsync(cancellationToken);
}
