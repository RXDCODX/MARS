using MARS.TestKit.Postgres;
using MARS.TwitchCore.Data;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Контекст Twitch поверх живой PostgreSQL: своя база на тест и схема из миграций.
///
/// На обходном провайдере проверялись только запросы к собранной модели: у
/// Husband внешний ключ на TwitchUser с обязательной навигацией, `Include` в
/// провайдере в памяти вел себя иначе, чем в PostgreSQL, и миграции Twitch
/// не применялись ни разу.
/// </summary>
internal sealed class TwitchTestDbContextFactory : PostgresTestDbContextFactory<TwitchDbContext>;
