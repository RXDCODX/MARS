using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;
using MARS.WaifuGacha.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Controllers;

public record WaifuRollAudioDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string FileExtension { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}
