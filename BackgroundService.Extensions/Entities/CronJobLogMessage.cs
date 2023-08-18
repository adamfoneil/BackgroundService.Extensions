using System.ComponentModel.DataAnnotations.Schema;

namespace BackgroundServiceExtensions.Entities;

public enum MessageType
{
    Info,
    Error,
    Json
}

public class CronJobLogMessage
{
    [NotMapped]
    public long Id { get; set; }
    public string JobName { get; set; } = default!;
    public int ExecutionId { get; set; }
    public DateTime Timestamp { get; set; }
    public MessageType MessageType { get; set; }
    public string Message { get; set; } = default!;
    public string Data { get; set; } = default!;
}
