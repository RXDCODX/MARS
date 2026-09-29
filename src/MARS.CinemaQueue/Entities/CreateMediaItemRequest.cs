namespace MARS.CinemaQueue.Entities;

public class CreateMediaItemRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public required string MediaUrl { get; set; }
    public int Priority { get; set; } = 0;
    public DateTime? ScheduledFor { get; set; }
    public string? TwitchUserId { get; set; }
    public string? Notes { get; set; }
}
