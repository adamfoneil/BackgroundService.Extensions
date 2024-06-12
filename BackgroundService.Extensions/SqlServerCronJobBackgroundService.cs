using BackgroundServiceExtensions.Entities;
using Cronos;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sgbj.Cron;
using SimpleCrud;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace BackgroundServiceExtensions;

public abstract class SqlServerCronJobBackgroundService<TResult> : BackgroundService
{
	protected readonly ILogger<SqlServerCronJobBackgroundService<TResult>> Logger;

	public SqlServerCronJobBackgroundService(ILogger<SqlServerCronJobBackgroundService<TResult>> logger)
	{
		Logger = logger;
	}

	public bool Enabled { get; set; } = true;

	public abstract string CrontabExpression { get; }

	protected abstract IDbConnection GetConnection();

	protected abstract string TableName { get; }

	protected virtual string SequenceName => "[dbo].[seq_CronJobExecution]";

	public abstract TimeZoneInfo TimeZone { get; }

	public async Task RunManualAsync(CancellationToken cancellationToken) => await ExecuteInnerAsync(cancellationToken, ExecutionType.Manual);

	protected abstract Task<(JobStatus Status, TResult Data)> DoWorkAsync(CancellationToken stoppingToken, CronJobInfo jobInfo);

	protected async Task UpdateJobInfoAsync(CronJobInfo jobInfo)
	{
		try
		{
			using var cn = GetConnection();

			var update = SqlServer.Update<CronJobInfo>(TableName);
			int count = await cn.ExecuteAsync(update, jobInfo);
			if (count == 0)
			{
				var insert = SqlServer.Insert<CronJobInfo>(TableName);
				await cn.ExecuteAsync(insert, jobInfo);
			}
		}
		catch (Exception exc)
		{
			Logger.LogError(exc, "Error in SqlServerCronJobBackgroundService.UpdateJobInfoAsync");
			throw;
		}
	}

	protected virtual string JobName => this.GetType().Name;

	protected async Task<int> GetNextExecutionIdAsync()
	{
		using var cn = GetConnection();
		return await cn.QuerySingleAsync<int>($"SELECT NEXT VALUE FOR {SequenceName}");
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (!Enabled) return;

		using var timer = new CronTimer(CrontabExpression, TimeZone);

		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			await ExecuteInnerAsync(stoppingToken, ExecutionType.Scheduled);
		}
	}

	private async Task ExecuteInnerAsync(CancellationToken stoppingToken, ExecutionType executionType)
	{
		var jobInfo = new CronJobInfo()
		{
			JobName = JobName,
			ExecutionId = await GetNextExecutionIdAsync(),
			Status = JobStatus.Running,
			Started = LocalTime(),
			ExecutionType = executionType
		};

		await UpdateJobInfoAsync(jobInfo);

		var sw = Stopwatch.StartNew();
		try
		{
			var result = await DoWorkAsync(stoppingToken, jobInfo);
			jobInfo.Status = result.Status;
			jobInfo.Succeeded = LocalTime();
			jobInfo.ResultData = JsonSerializer.Serialize(result.Data);
		}
		catch (Exception exc)
		{
			Logger.LogError(exc, "Error in SqlServerCronJobBackgroundService.ExecuteInnerAsync");
			jobInfo.Status = JobStatus.Exception;
			jobInfo.Failed = LocalTime();
			jobInfo.ErrorMessage = exc.Message;
		}
		finally
		{
			sw.Stop();
			jobInfo.Duration = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
			jobInfo.NextOccurence = GetNextOccurrence();
			await UpdateJobInfoAsync(jobInfo);
		}

		DateTime LocalTime() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);
	}

	private DateTime? GetNextOccurrence()
	{
		var expr = CronExpression.Parse(CrontabExpression);
		var nextOccurrence = expr.GetNextOccurrence(DateTime.UtcNow, TimeZone) ?? throw new Exception("Couldn't get the next occurrence");
		return TimeZoneInfo.ConvertTimeFromUtc(nextOccurrence, TimeZone);
	}

	public static string TableSql(string tableName) =>
		$@"CREATE TABLE {tableName} (
			[Id] int identity(1,1) PRIMARY KEY,
			[JobName] nvarchar(100) NOT NULL,
			[ExecutionId] int NOT NULL,
			[Status] int NOT NULL,
			[ExecutionType] int NOT NULL,
			[NextOccurence] datetime NULL,
			[Started] datetime NULL,
			[Succeeded] datetime NULL,
			[Failed] datetime NULL,            
			[ResultData] nvarchar(max) NULL,    
			[ErrorMessage] nvarchar(max) NULL,
			[Duration] time NULL,            
			CONSTRAINT [U_CronJobInfo_JobName] UNIQUE ([JobName])
		)";

	public static string SequenceSql(string sequenceName) =>
		$"CREATE SEQUENCE {sequenceName} START WITH 1 INCREMENT BY 1";
}
