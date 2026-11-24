using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Platform.Infrastructure.Postgres.Extensions;

/// <summary>
/// Готовность схемы БД: позволяет IHostedService дождаться применения миграций
/// своего контекста.
/// Аудит (runtime-проверка compose): миграции выполнялись фоновым сервисом, их
/// ExecuteAsync стартовал параллельно с остальными сервисами. MARS.TwitchCore и
/// MARS.SoundRequest читали свои таблицы раньше миграций и падали с
/// 42P01: relation "twitch.RootState" does not exist → restart-loop.
/// Теперь миграции применяются синхронно до старта хоста
/// (<c>RunMarsSchemaMigrationsAsync</c>),
/// а этот интерфейс остаётся для сервисов, которые стартуют позже динамически.
/// </summary>
public interface IMarsSchemaReady<TContext>
    where TContext : DbContext
{
    Task WaitAsync(CancellationToken cancellationToken);
}
