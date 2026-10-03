using MARS.Alerts.Data;
using MARS.TestKit.Postgres;

namespace MARS.Alerts.Tests;

/// <summary>
/// Контекст Alerts поверх живой PostgreSQL: своя база на тест и схема из миграций.
/// </summary>
internal sealed class AlertsTestDbContextFactory : PostgresTestDbContextFactory<AlertsDbContext>;
