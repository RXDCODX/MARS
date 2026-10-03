namespace MARS.SoundRequest.Hubs.Dtos;

/// <summary>Элемент очереди в форме REST-контракта клиента.</summary>
public class QueueItemHubDto
{
    public string Id { get; set; } = string.Empty;

    public TrackInfoHubDto? Track { get; set; }
}
