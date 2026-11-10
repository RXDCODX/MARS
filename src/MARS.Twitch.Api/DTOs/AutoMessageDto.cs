namespace MARS.Twitch.Api.DTOs;

public class AutoMessageDto
{
    public Guid Id { get; set; }
    public required string Message { get; set; }
}
