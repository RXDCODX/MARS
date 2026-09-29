namespace MARS.TwitchCore.DTOs;

public class AutoMessageDto
{
    public Guid Id { get; set; }
    public required string Message { get; set; }
}

public class CreateAutoMessageRequest
{
    public required string Message { get; set; }
}

public class UpdateAutoMessageRequest
{
    public string? Message { get; set; }
}
