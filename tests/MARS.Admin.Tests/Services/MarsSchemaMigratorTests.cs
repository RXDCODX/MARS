using MARS.Admin.Data;
using MARS.Platform.Persistence.Postgres.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace MARS.Admin.Tests.Services;

/// <summary>
/// Проверка статического конструктора <see cref="MarsSchemaMigrator{TContext}"/>
/// — он покрывается при первом создании экземпляра любого закрытого типа.
/// </summary>
public class MarsSchemaMigratorTests
{
    [Fact]
    public void StaticConstructorIsInvokedOnFirstInstance()
    {
        var factory = new Mock<IDbContextFactory<AdminDbContext>>();
        var migrator = new MarsSchemaMigrator<AdminDbContext>(factory.Object);

        Assert.NotNull(migrator);
    }
}
