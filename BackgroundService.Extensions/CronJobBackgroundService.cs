using Cronos;
using Microsoft.Extensions.Hosting;
using Sgbj.Cron;
using System.Diagnostics;
using System.Text.Json;

namespace BackgroundServiceExtensions;

public abstract class CronJobBackgroundService<TJobInfo, TResultData> : BackgroundService where TJobInfo : ICronJobInfo, new()
{
	public CronJobBackgroundService()
	{
	}

	public abstract string CrontabExpression { get; }

	public abstract TimeZoneInfo TimeZone { get; }

	public async Task RunManualAsync(CancellationToken cancellationToken) => await ExecuteInnerAsync(cancellationToken, true);

	protected abstract Task<(JobStatus Status, TResultData Data)> DoWorkAsync(CancellationToken stoppingToken, ICronJobInfo jobInfo);

	protected abstract Task UpdateJobInfoAsync(TJobInfo jobInfo);

	protected virtual string JobName => this.GetType().Name;

	protected abstract Task<int> GetNextExecutionIdAsync();

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new CronTimer(CrontabExpression, TimeZone);
		
		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			await ExecuteInnerAsync(stoppingToken, false);
		}
	}	

	private async Task ExecuteInnerAsync(CancellationToken stoppingToken, bool runningManually)
	{
		var jobInfo = new TJobInfo() 
		{ 
			JobName = JobName,
			ExecutionId = await GetNextExecutionIdAsync(),
			Status = JobStatus.Running,
			Started = new DateTimeOffset(DateTime.UtcNow, TimeZone.BaseUtcOffset),			
			RunManually = runningManually
		};

		await UpdateJobInfoAsync(jobInfo);

		var sw = Stopwatch.StartNew();
		try
		{
			var result = await DoWorkAsync(stoppingToken, jobInfo);						
			jobInfo.Status = JobStatus.Succeeded;
			jobInfo.Succeeded = new DateTimeOffset(DateTime.UtcNow, TimeZone.BaseUtcOffset);
			jobInfo.ResultData = JsonSerializer.Serialize(result.Data);
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
