namespace Testing.Models;

public class QueueItem
{
	public int Id { get; set; }
	public DateTime Timestamp { get; set; }
	public string Message { get; set; } = default!;
}
