namespace BackgroundServiceExtensions.Entities;

public enum JobStatus
{
    Pending,
    Running,
    Succeeded,
    Failed
}

public enum ExecutionType
{
    Scheduled,
    Manual
}

public class CronJobInfo
{
    public int Id { get; set; }
    public string JobName { get; set; } = default!;
    /// <summary>
    /// value for correlating log detail with the latest execution of a job
    /// </summary>
    public int ExecutionId { get; set; }
    public JobStatus Status { get; set; }
    public ExecutionType ExecutionType { get; set; }
    /// <summary>
    /// when is the next run of this job
    /// </summary>
    public DateTimeOffset? NextOccurence { get; set; }
    public DateTimeOffset? Started { get; set; }
    public DateTimeOffset? Succeeded { get; set; }
    /// <summary>
    /// json output of the last run
    /// </summary>
    public string? ResultData { get; set; }
    public DateTimeOffset? Failed { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? Duration { get; set; }
}
