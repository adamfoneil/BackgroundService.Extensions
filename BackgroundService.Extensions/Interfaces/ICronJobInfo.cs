namespace HostedService.Extensions;

public enum JobStatus
{
	Pending,
	Running,
	Succeeded,
	Failed
}

public interface ICronJobInfo
{
	string JobName { get; set; }
	/// <summary>
	/// value for correlating log detail with the latest execution of a job
	/// </summary>
	int ExecutionId { get; set; }
	JobStatus Status { get; set; }
	/// <summary>
	/// if true, the job was invoked manually instead of based on its cron expression
	/// </summary>
	bool RunManually { get; set; }
	/// <summary>
	/// when is the next run of this job
	/// </summary>
	DateTimeOffset? NextOccurence { get; set; }
	DateTimeOffset? Started { get; set; }
	DateTimeOffset? Succeeded { get; set; }
	string? ResultData { get; set; }
	DateTimeOffset? Failed { get; set; }
	string? ErrorMessage { get; set; }
	TimeSpan? Duration { get; set; }
}
