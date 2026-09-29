namespace MARS.Shared.Hubs.Models;

public class HusbandAlert
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}
