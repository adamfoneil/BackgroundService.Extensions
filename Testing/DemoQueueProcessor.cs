using BackgroundServiceExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;
using Testing.Models;

namespace Testing;

/// <summary>
/// a real queue processor class would probably have a TData more sophisticated than just strings.
/// This is a minimal implementation for emitting simple messages only.
/// </summary>
public class DemoQueueProcessor : SqlServerQueueBackgroundService<QueueItem, string>
{
	private readonly string _connectionString;

	public DemoQueueProcessor(string connectionString, ILogger<DemoQueueProcessor> logger) : base(logger)
	{
		_connectionString = connectionString;
	}

	protected override string QueueTableName => "[dbo].[Queue]";

	protected override string ErrorTableName => "[dbo].[Error]";

	/// <summary>
	/// you likely would not have a property like this in a real app.
	/// This is just for triggering internal error logging behavior
	/// </summary>
	public bool SimulateError { get; set; }

	protected override IDbConnection GetConnection() => new SqlConnection(_connectionString);

	protected override async Task DoWorkAsync(CancellationToken stoppingToken, DateTime started, QueueItem message, string? data)
	{
		if (!SimulateError)
		{
			Debug.Print($"item Id {message.Id} received on {message.Queued}: {data}");
			await Task.CompletedTask;
			return;
		}

		throw new Exception("Just testing the error behavior");
	}
}
