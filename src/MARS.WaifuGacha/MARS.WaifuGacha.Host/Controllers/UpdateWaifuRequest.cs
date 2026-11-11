using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Domain.Entities;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Controllers;

public record UpdateWaifuRequest
{
    public string? Name { get; init; }
    public long? Age { get; init; }
    public string? Anime { get; init; }
    public string? Manga { get; init; }
    public string? ImageUrl { get; init; }
    public bool? IsPrivated { get; init; }
    public Guid? AudioId { get; init; }
}
