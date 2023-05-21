using BackgroundServiceExtensions.Interfaces;

namespace Testing.Models;

public class QueueItem : IQueueMessage
{
	public long Id { get; set; }
    public DateTime Queued { get; set; }
    public string UserName { get; set; } = default!;
    public string Data { get; set; } = default!;        
}
