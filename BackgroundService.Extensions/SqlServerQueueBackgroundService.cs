using BackgroundServiceExtensions.Interfaces;
using Dapper;
using Microsoft.Extensions.Hosting;
using System.Data;
using System.Text.Json;

namespace BackgroundServiceExtensions;

/// <summary>
/// help from http://rusanu.com/2010/03/26/using-tables-as-queues/ Heap Queues
/// </summary>
public abstract class SqlServerQueueBackgroundService<TMessage, TData> : BackgroundService where TMessage : IQueueMessage, new() where TData : notnull
{
	protected abstract IDbConnection GetConnection();

	protected abstract string QueueTableName { get; }

	protected abstract string ErrorTableName { get; }

	protected abstract Task DoWorkAsync(CancellationToken stoppingToken, DateTime started, TMessage message, TData? data);

	public async Task<long> EnqueueAsync(string userName, TData data)
	{
        ArgumentNullException.ThrowIfNull(userName, nameof(userName));
        ArgumentNullException.ThrowIfNull(data, nameof(data));

		var message = new TMessage();
		message.UserName = userName;
		message.Queued = DateTime.UtcNow;
		message.Data = JsonSerializer.Serialize(data);
		message.Type = typeof(TData).Name;

        using var cn = GetConnection();
        return await cn.QuerySingleAsync<long>(
            $@"INSERT INTO {QueueTableName} ([Queued], [Type], [UserName], [Data]) VALUES (getdate(), @type, @userName, @data);
            SELECT SCOPE_IDENTITY()",
            message);
    }

	/// <summary>
	/// normally you would not call this yourself, but rather let it be called by the background service.
	/// This is public for testing purposes only.
	/// </summary>
    public async Task DequeueAsync(CancellationToken stoppingToken)
	{
		using var cn = GetConnection();

		TMessage message = await cn.QuerySingleOrDefaultAsync<TMessage>(
			$"DELETE TOP (1) FROM {QueueTableName} WITH (ROWLOCK, READPAST) OUTPUT [deleted].* WHERE [Type]=@type",
			new { type = typeof(TData).Name });

		if (message is not null)
		{			
			var data = JsonSerializer.Deserialize<TData>(message.Data);
			try
			{
				await DoWorkAsync(stoppingToken, DateTime.UtcNow, message, data);
			}
			catch (Exception exc)
			{
				await LogErrorAsync(exc, message, data);
			}
		}
	}

	public static string QueueTableSql(string tableName) =>
		$@"CREATE TABLE {tableName} (
			[Id] bigint identity(1,1) PRIMARY KEY,
			[UserName] nvarchar(50) NOT NULL,
			[Queued] datetime NOT NULL,
			[Type] nvarchar(50) NOT NULL,
			[Data] nvarchar(max) NOT NULL
		);";

	public static string ErrorTableSql(string tableName) => 
		$@"CREATE TABLE {tableName} (
			[Id] bigint identity(1,1) PRIMARY KEY,
			[Timestamp] datetime NOT NULL DEFAULT (getdate()),			
			[ErrorMessage] nvarchar(max) NOT NULL,
			[QueueMessage] nvarchar(max) NOT NULL,
			[Data] nvarchar(max) NOT NULL
		);";

    private async Task LogErrorAsync(Exception exception, TMessage item, TData? data)
	{
		using var cn = GetConnection();

		item.Data = string.Empty; // because it will be redundant to the Data value

		try
		{
			await cn.ExecuteAsync($"INSERT INTO {ErrorTableName} ([ErrorMessage], [QueueMessage], [Data]) VALUES (@errorMessage, @queueMessage, @data)", new
			{
				errorMessage = exception.Message,
				queueMessage = JsonSerializer.Serialize(item),
				data = JsonSerializer.Serialize(data)
			});
		}
		catch (Exception exc)
		{
			throw new Exception($"Error logging queue process failure: {exc.Message}", exc);
		}
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{		
		while (!stoppingToken.IsCancellationRequested)
		{
			await DequeueAsync(stoppingToken);			
		}		
	}
}
