using MARS.Platform.Clients.Http.Infrastructure;
using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class AddNewWaifuResult
{
    public Waifu? Waifu { get; set; }
    public string? DisplayName { get; set; }
    public bool VipDropped { get; set; }
    public int RollsUntilGuarantee { get; set; }
}
