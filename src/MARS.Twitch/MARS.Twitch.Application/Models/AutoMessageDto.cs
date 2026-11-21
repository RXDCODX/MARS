namespace MARS.Twitch.Application.Models;

public class AutoMessageDto
{
    public Guid Id { get; set; }
    public required string Message { get; set; }
}
