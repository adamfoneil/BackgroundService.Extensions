using BackgroundServiceExtensions.Entities;
using Cronos;
using Dapper;
using Microsoft.Extensions.Hosting;
using Sgbj.Cron;
using SimpleCrud;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace BackgroundServiceExtensions;

public abstract class SqlServerCronJobBackgroundService<TResult> : BackgroundService
{
    public SqlServerCronJobBackgroundService()
    {
    }

    private CronJobInfo _currentJob = new();

    public abstract string CrontabExpression { get; }

    protected abstract IDbConnection GetConnection();
    
    protected abstract string TableName { get; }

    protected abstract string LogTableName { get; }

    protected virtual string SequenceName => "[dbo].[seq_CronJobExecution]";

    public abstract TimeZoneInfo TimeZone { get; }

    public async Task RunManualAsync(CancellationToken cancellationToken) => await ExecuteInnerAsync(cancellationToken, ExecutionType.Manual);

    protected abstract Task<(JobStatus Status, TResult Data)> DoWorkAsync(CancellationToken stoppingToken, CronJobInfo jobInfo);

    protected async Task UpdateJobInfoAsync(CronJobInfo jobInfo)
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

    protected virtual string JobName => this.GetType().Name;

    protected async Task<int> GetNextExecutionIdAsync()
    {
        using var cn = GetConnection();
        return await cn.QuerySingleAsync<int>($"SELECT NEXT VALUE FOR {SequenceName}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new CronTimer(CrontabExpression, TimeZone);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExecuteInnerAsync(stoppingToken, ExecutionType.Scheduled);
        }
    }    

    protected async Task LogInfoAsync(string message) => await LogAsync(MessageType.Info, message);

    protected async Task LogErrorAsync(Exception exception) => await LogAsync(MessageType.Error, exception.Message);

    private async Task LogAsync(MessageType messageType, string message)
    {
        var logMessage = new CronJobLogMessage()
        {
            JobName = _currentJob.JobName,
            ExecutionId = _currentJob.ExecutionId,
            MessageType = messageType,
            Message = message
        };

        var sql = SqlServer.Insert<CronJobLogMessage>(LogTableName);
        using var cn = GetConnection();        
        await cn.ExecuteAsync(sql, message);
    }

    private async Task ExecuteInnerAsync(CancellationToken stoppingToken, ExecutionType executionType)
    {
        _currentJob = new CronJobInfo()
        {
            JobName = JobName,
            ExecutionId = await GetNextExecutionIdAsync(),
            Status = JobStatus.Running,
            Started = LocalTime(),
            ExecutionType = executionType
        };

        await UpdateJobInfoAsync(_currentJob);

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await DoWorkAsync(stoppingToken, _currentJob);
            _currentJob.Status = result.Status;
            _currentJob.Succeeded = LocalTime();
            _currentJob.ResultData = JsonSerializer.Serialize(result.Data);
        }
        catch (Exception exc)
        {
            _currentJob.Status = JobStatus.Exception;
            _currentJob.Failed = LocalTime();
            _currentJob.ErrorMessage = exc.Message;
        }
        finally
        {
            sw.Stop();
            _currentJob.Duration = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
            _currentJob.NextOccurence = GetNextOccurrence();
            await UpdateJobInfoAsync(_currentJob);
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

    public static string LogTableSql(string tableName) =>
        $@"CREATE TABLE {tableName} (
            [Id] bigint identity(1,1) PRIAMRY KEY,
            [JobName] nvarchar(100) NOT NULL,
            [ExecutionId] int NOT NULL,
            [Timestamp] datetime NOT NULL,
            [MessageType] int NOT NULL,
            [Message] nvarchar(max) NOT NULL,
            [Data] nvarchar(max) NULL
        )";
}
