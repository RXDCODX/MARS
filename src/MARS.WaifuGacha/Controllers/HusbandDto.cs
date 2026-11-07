using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Controllers;

public record HusbandDto
{
    public string TwitchId { get; init; } = "";
    public DateTime WhenOrdered { get; init; }
    public string? WaifuBrideId { get; init; }
    public bool IsPrivated { get; init; }
    public long OrderCount { get; init; }
    public string? WaifuRollId { get; init; }
    public DateTime? WhenPrivated { get; init; }
    public int? LastWeddingCongratulatedMonths { get; init; }
}
