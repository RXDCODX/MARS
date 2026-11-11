using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;
using MARS.WaifuGacha.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Controllers;

public record CreateWaifuRequest
{
    public string ShikiId { get; init; } = "";
    public string Name { get; init; } = "";
    public long Age { get; init; }
    public string? Anime { get; init; }
    public string? Manga { get; init; }
    public string? ImageUrl { get; init; }
    public Guid? AudioId { get; init; }
}
