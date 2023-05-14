namespace AO.HostedService.Extensions;

public enum JobStatus
{
	Pending,
	Running,
	Succeeded,
	Failed
}

public interface IJobInfo
{
	string JobName { get; set; }
	JobStatus Status { get; set; }
	bool RunManually { get; set; }
	DateTimeOffset? NextOccurence { get; set; }
	DateTimeOffset? Started { get; set; }
	DateTimeOffset? Succeeded { get; set; }
	string? ResultData { get; set; }
	DateTimeOffset? Failed { get; set; }
	string? ErrorMessage { get; set; }
	TimeSpan? Duration { get; set; }
}
