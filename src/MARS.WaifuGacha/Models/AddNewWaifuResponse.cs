using MARS.WaifuGacha.Entities;

namespace MARS.WaifuGacha.Models;

public class AddNewWaifuResponse
{
    public Waifu? Waifu { get; set; }
    public bool HasError { get; set; }
}
