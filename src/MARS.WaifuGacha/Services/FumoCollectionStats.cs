using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public record FumoCollectionStats
{
    public int CollectedCount { get; init; }
    public int TotalCount { get; init; }
    public int ThisItemCount { get; init; }
    public bool IsNew { get; init; }
    public bool GuaranteeTriggered { get; init; }
    public int? GuaranteedItemId { get; init; }
}
