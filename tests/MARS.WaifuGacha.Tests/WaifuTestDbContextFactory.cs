using MARS.TestKit.Postgres;
using MARS.WaifuGacha.Data;

namespace MARS.WaifuGacha.Tests;

/// <summary>
/// Контекст WaifuGacha поверх живой PostgreSQL: своя база на тест и настоящие
/// миграции.
///
/// Раньше здесь стояли два обходных варианта — InMemory для большинства тестов и
/// SQLite для размоножения, где запрос идёт через <c>EF.Functions.ILike</c>.
/// Оба проверяли модель, а не PostgreSQL: на InMemory не работает
/// <c>ExecuteUpdateAsync</c>, а SQLite не переводит <c>ILIKE</c> вовсе. Теперь
/// путь один, и он настоящий.
/// </summary>
internal sealed class WaifuTestDbContextFactory : PostgresTestDbContextFactory<WaifuDbContext>;
