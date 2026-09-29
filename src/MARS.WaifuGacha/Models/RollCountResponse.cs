namespace MARS.WaifuGacha.Models;

public class RollCountResponse
{
    public bool Success { get; set; }
    public int CurrentRollCount { get; set; }
    public string? Message { get; set; }
}
