using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Domain.Entities;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Controllers;

public record UpdateHusbandRequest
{
    public string? WaifuBrideId { get; init; }
    public bool? IsPrivated { get; init; }
    public string? WaifuRollId { get; init; }
    public DateTime? WhenPrivated { get; init; }
    public int? LastWeddingCongratulatedMonths { get; init; }
}
