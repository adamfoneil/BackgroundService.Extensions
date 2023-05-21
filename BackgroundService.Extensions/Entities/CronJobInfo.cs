using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackgroundServiceExtensions.Entities;

public enum JobStatus
{
    NotSet,
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
    [NotMapped]
    public int Id { get; set; }
    [Key]
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
    public DateTime? NextOccurence { get; set; }
    public DateTime? Started { get; set; }
    public DateTime? Succeeded { get; set; }
    /// <summary>
    /// json output of the last run
    /// </summary>
    public string? ResultData { get; set; }
    public DateTime? Failed { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? Duration { get; set; }    
}
