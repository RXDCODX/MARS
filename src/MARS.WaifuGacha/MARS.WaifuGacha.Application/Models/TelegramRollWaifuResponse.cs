using MARS.WaifuGacha.Domain.Entities;

namespace MARS.WaifuGacha.Application.Models;

public class TelegramRollWaifuResponse
{
    public Waifu? Waifu { get; set; }
    public Husband? Host { get; set; }
    public Husband? Husband { get; set; }
}
