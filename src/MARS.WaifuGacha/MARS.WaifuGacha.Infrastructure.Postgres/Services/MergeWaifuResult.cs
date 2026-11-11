using System.Collections.Concurrent;
using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;
using MARS.WaifuGacha.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Infrastructure.Postgres.Services;

public class MergeWaifuResult
{
    public Waifu? Waifu { get; set; }
    public Husband? Host { get; set; }
    public bool IsNewMarriage { get; set; }
}
