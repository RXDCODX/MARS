namespace MARS.CinemaQueue.Application.Models;

public class CinemaQueueStatistics
{
    public int TotalItems { get; set; }
    public int PendingItems { get; set; }
    public int InProgressItems { get; set; }
    public int CompletedItems { get; set; }
    public int CancelledItems { get; set; }
    public int PostponedItems { get; set; }
}
