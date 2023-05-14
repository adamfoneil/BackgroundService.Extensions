using Cronos;
using Sgbj.Cron;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace AO.HostedService.Extensions;

public abstract class CronJobBackgroundService<T> : BackgroundService where T : IJobInfo, new()
{
	public CronJobBackgroundService()
	{
	}

	public abstract string CrontabExpression { get; }

	public abstract TimeZoneInfo TimeZone { get; }

	public async Task ManualExecuteAsync(CancellationToken cancellationToken) => await ExecuteInnerAsync(cancellationToken, true);

	protected abstract Task<(JobStatus Status, string Data)> DoWorkAsync(CancellationToken stoppingToken);

	protected abstract Task UpdateJobInfoAsync(T jobInfo);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new CronTimer(CrontabExpression, TimeZone);
		
		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			await ExecuteInnerAsync(stoppingToken, false);
		}
	}

	protected virtual async Task LogInfoAsync(string level, string message, string detail) => await Task.CompletedTask;

	private async Task ExecuteInnerAsync(CancellationToken stoppingToken, bool runningManually)
	{
		var jobInfo = new T() 
		{ 
			JobName = this.GetType().Name, 
			Status = JobStatus.Running,
			Started = new DateTimeOffset(DateTime.UtcNow, TimeZone.BaseUtcOffset),			
			RunManually = runningManually
		};

		await UpdateJobInfoAsync(jobInfo);

		var sw = Stopwatch.StartNew();
		try
		{								
			var result = await DoWorkAsync(stoppingToken);						
			jobInfo.Status = JobStatus.Succeeded;
			jobInfo.Succeeded = new DateTimeOffset(DateTime.UtcNow, TimeZone.BaseUtcOffset);
			jobInfo.ResultData = result.Data;			
		}
		catch (Exception exc)
		{
			jobInfo.Status = JobStatus.Failed;
			jobInfo.Failed = new DateTimeOffset(DateTime.UtcNow, TimeZone.BaseUtcOffset);
			jobInfo.ErrorMessage = exc.Message;
		}
		finally
		{
			sw.Stop();
			jobInfo.Duration = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
			jobInfo.NextOccurence = GetNextOccurrence();			
			await UpdateJobInfoAsync(jobInfo);
		}
	}

	private DateTime? GetNextOccurrence()
	{
		var expr = CronExpression.Parse(CrontabExpression);
		return expr.GetNextOccurrence(DateTime.UtcNow, TimeZone);
	}
}
