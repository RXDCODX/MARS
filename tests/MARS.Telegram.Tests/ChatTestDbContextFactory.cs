using MARS.Telegram.Data;
using MARS.TestKit.Postgres;

namespace MARS.Telegram.Tests;

/// <summary>
/// Фабрика контекстов чата поверх живой PostgreSQL: своя база на тест и схема из
/// миграций.
///
/// Раньше база жила в памяти процесса, и уникальное имя на экземпляр изолировало
/// тесты только по имени. Теперь изоляция настоящая, а заодно применяются
/// миграции чата — на обходном провайдере они не проверялись вовсе.
/// </summary>
internal sealed class ChatTestDbContextFactory : PostgresTestDbContextFactory<ChatDbContext>;
