using System.Collections.Concurrent;
using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class MergeWaifuResult
{
    public Waifu? Waifu { get; set; }
    public Husband? Host { get; set; }
    public bool IsNewMarriage { get; set; }
}
