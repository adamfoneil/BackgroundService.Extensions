using BackgroundServiceExtensions.Interfaces;

namespace Testing.Models;

public class QueueItem : IQueueMessage
{
	public long Id { get; set; }
    public DateTime Queued { get; set; }
    public string UserName { get; set; } = default!;
    public string Type { get; set; } = default!;
    /// <summary>
    /// raw json version of queued data
    /// </summary>
    public string Data { get; set; } = default!;        
}
