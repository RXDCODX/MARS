namespace MARS.Shared.Contracts.SoundRequest;

public class SoundRequestEvent
{
    public PlayerStateSnapshot? PlayerStateChange { get; set; }
    public QueueState? QueueChanged { get; set; }

    /// <summary>Имя заполненной ветки; раньше — enum и цепочка проверок.</summary>
    public string? EventCase => OneofEventContract.GetActiveBranch(this)?.Name;
}
