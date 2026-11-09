using MARS.Admin.Domain.Entities;
using MARS.Admin.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MARS.Admin.Tests.Services;

/// <summary>
/// Проверяет PostgreSQL-адаптер отдельно от ServiceManager: прикладной порт
/// должен сохранять изменение состояния через DbContext с отключённым tracking.
/// </summary>
public class AdminServiceStateRepositoryTests
{
    [Fact]
    public async Task ActiveStateIsPersistedForExistingService()
    {
        var factory = new AdminDbContextFactory();
        await using (
            var seed = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
        )
        {
            seed.ServiceStates.Add(
                new ServiceState
                {
                    ServiceName = "obs",
                    IsServiceActive = true,
                    Status = ServiceStatus.Stopped,
                }
            );
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var repository = new AdminServiceStateRepository(factory);

        await repository.SetServiceActiveAsync("obs", false);

        await using var verify = await factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var state = await verify.ServiceStates.SingleAsync(TestContext.Current.CancellationToken);

        Assert.False(state.IsServiceActive);
    }
}
