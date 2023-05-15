using HostedService.Extensions;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using Testing.Models;

namespace Testing;

public class DemoQueueProcessor : SqlServerQueueBackgroundService<QueueItem>
{
	private readonly string _connectionString;

    public DemoQueueProcessor(string connectionString)
    {        
		_connectionString = connectionString;
    }

    protected override string QueueTableName => "[dbo].[Queue]";

	protected override string ErrorTableName => "[dbo].[Error]";

	public bool Throw { get; set; }

	protected override async Task DoWorkAsync(CancellationToken stoppingToken, QueueItem item)
	{		
		if (!Throw)
		{
			Debug.Print($"item Id {item.Id} received on {item.Timestamp}: {item.Message}");
			await Task.CompletedTask;
			return;
		}

		throw new Exception("Just testing the error behavior");
	}

	protected override IDbConnection GetConnection() => new SqlConnection(_connectionString);
	
}
