using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Controllers;

public record WaifuDto
{
    public string ShikiId { get; init; } = "";
    public string Name { get; init; } = "";
    public long Age { get; init; }
    public string? Anime { get; init; }
    public string? Manga { get; init; }
    public DateTime WhenAdded { get; init; }
    public DateTime LastOrder { get; init; }
    public int OrderCount { get; init; }
    public bool IsPrivated { get; init; }
    public string ImageUrl { get; init; } = "";
    public Guid? AudioId { get; init; }
    public string? AudioName { get; init; }
}
