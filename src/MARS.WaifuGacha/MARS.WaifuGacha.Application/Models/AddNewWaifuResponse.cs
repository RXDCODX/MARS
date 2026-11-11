using MARS.WaifuGacha.Domain.Entities;

namespace MARS.WaifuGacha.Application.Models;

public class AddNewWaifuResponse
{
    public Waifu? Waifu { get; set; }
    public bool HasError { get; set; }
}
