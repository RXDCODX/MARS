using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Domain.Entities;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Infrastructure.Postgres.Services;

public record MikuCollectionStats
{
    public int CollectedCount { get; init; }
    public int TotalCount { get; init; }
    public int ThisModuleCount { get; init; }
    public bool IsNew { get; init; }
    public bool GuaranteeTriggered { get; init; }
    public int? GuaranteedPageId { get; init; }
}
