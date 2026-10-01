namespace MARS.Shared.Grpc.Models;

public class WaifuAlert
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? ImageUrl { get; set; }
    public string? Source { get; set; }
}
