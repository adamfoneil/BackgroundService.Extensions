using BackgroundServiceExtensions;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using Testing.Models;

namespace Testing;

public class DemoQueueProcessor : SqlServerQueueBackgroundService<QueueItem, string>
{
	private readonly string _connectionString;

    public DemoQueueProcessor(string connectionString)
    {        
		_connectionString = connectionString;
    }

    protected override string QueueTableName => "[dbo].[Queue]";

	protected override string ErrorTableName => "[dbo].[Error]";

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

    protected override async Task<long> EnqueueInternalAsync(QueueItem message)
    {
        using var cn = GetConnection();
        return await cn.QuerySingleAsync<long>(
            @"INSERT INTO [dbo].[Queue] ([Queued], [Type], [UserName], [Data]) VALUES (getdate(), @type, @userName, @data);
            SELECT SCOPE_IDENTITY()",
            message);
    }
}
