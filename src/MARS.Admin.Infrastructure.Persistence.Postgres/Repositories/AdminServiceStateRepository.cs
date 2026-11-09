using MARS.Admin.Application.Services.ServiceManager;
using MARS.Admin.Infrastructure.Persistence.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.Admin.Infrastructure.Persistence.Postgres.Repositories;

public sealed class AdminServiceStateRepository(IDbContextFactory<AdminDbContext> dbContextFactory)
    : IServiceStateRepository
{
    public async Task SetServiceActiveAsync(string serviceName, bool isActive)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var state = await dbContext.ServiceStates.FirstOrDefaultAsync(s =>
            s.ServiceName == serviceName
        );

        if (state is null)
        {
            return;
        }

        state.IsServiceActive = isActive;
        state.UpdatedAt = DateTime.Now;
        dbContext.ServiceStates.Update(state);
        await dbContext.SaveChangesAsync();
    }
}
