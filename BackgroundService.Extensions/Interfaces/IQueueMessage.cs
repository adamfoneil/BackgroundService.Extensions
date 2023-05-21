namespace BackgroundServiceExtensions.Interfaces;

public interface IQueueMessage
{
    string UserName { get; set; }
    DateTime Queued { get; set; }
    string Type { get; set; }
    string Data { get; set; }
}
