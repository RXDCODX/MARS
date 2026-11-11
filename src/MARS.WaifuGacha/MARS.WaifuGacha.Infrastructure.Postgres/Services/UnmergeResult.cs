using System.Collections.Concurrent;
using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Domain.Entities;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Infrastructure.Postgres.Services;

public class UnmergeResult
{
    public Waifu? Waifu { get; set; }
    public Husband? Host { get; set; }
}
