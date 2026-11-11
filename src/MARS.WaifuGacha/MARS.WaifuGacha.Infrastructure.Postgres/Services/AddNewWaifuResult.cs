using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Infrastructure.Postgres.Services;

public class AddNewWaifuResult
{
    public Waifu? Waifu { get; set; }
    public string? DisplayName { get; set; }
    public bool VipDropped { get; set; }
    public int RollsUntilGuarantee { get; set; }
}
