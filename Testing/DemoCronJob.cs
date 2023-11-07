using BackgroundServiceExtensions;
using BackgroundServiceExtensions.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Testing;

internal class DemoJobResult
{
	public string Message { get; set; } = default!;
}

internal class DemoCronJob : SqlServerCronJobBackgroundService<DemoJobResult>
{
	private readonly string _connectionString;

	public DemoCronJob(string connectionString, ILogger<DemoCronJob> logger) : base(logger)
	{
		_connectionString = connectionString;
	}

	public override string CrontabExpression => "* * * * *";

	public override TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

	protected override string TableName => "dbo.CronJobInfo";

	protected override async Task<(JobStatus Status, DemoJobResult Data)> DoWorkAsync(CancellationToken stoppingToken, CronJobInfo jobInfo)
	{
		await Task.Delay(1000);

		var data = new DemoJobResult()
		{
			Message = "Job completed successfully."
		};

		return (JobStatus.Succeeded, data);
	}

	protected override IDbConnection GetConnection() => new SqlConnection(_connectionString);
	
}
