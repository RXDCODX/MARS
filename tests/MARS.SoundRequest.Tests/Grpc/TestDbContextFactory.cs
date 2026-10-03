using MARS.SoundRequest.Data;
using MARS.TestKit.Postgres;

namespace MARS.SoundRequest.Tests.Grpc;

/// <summary>
/// Фабрика контекстов поверх живой PostgreSQL: своя база на тест и схема из
/// миграций.
///
/// Раньше опции собирал вызывающий тест, и каждый указывал своё имя базы в
/// памяти. Теперь база одна на тест, и её создаёт фикстура: тесты медиастора
/// пишут через <c>ExecuteUpdateAsync</c> и читают составные навигации, а это
/// relational-only поведение, которое провайдер в памяти либо не умеет, либо
/// трактует иначе, чем PostgreSQL.
/// </summary>
public sealed class TestDbContextFactory : PostgresTestDbContextFactory<MediaDbContext>;
