using BackgroundServiceExtensions.Extensions;
using BackgroundServiceExtensions.Interfaces;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace BackgroundServiceExtensions;

/// <summary>
/// help from http://rusanu.com/2010/03/26/using-tables-as-queues/ Heap Queues
/// </summary>
public abstract class SqlServerQueueBackgroundService<TMessage, TData> : BackgroundService where TMessage : IQueueMessage, new() where TData : notnull
{
	protected readonly ILogger<SqlServerQueueBackgroundService<TMessage, TData>> Logger;

	public SqlServerQueueBackgroundService(ILogger<SqlServerQueueBackgroundService<TMessage, TData>> logger)
	{
		Logger = logger;
	}

	protected abstract IDbConnection GetConnection();

	protected abstract string QueueTableName { get; }

	protected abstract string ErrorTableName { get; }

	protected abstract Task DoWorkAsync(DateTime started, TMessage message, TData? data, CancellationToken stoppingToken);

	public async Task<long> EnqueueAsync(string userName, TData data)
	{
		ArgumentNullException.ThrowIfNull(userName, nameof(userName));
		ArgumentNullException.ThrowIfNull(data, nameof(data));

		var message = new TMessage
		{
			UserName = userName,
			Queued = DateTime.UtcNow,
			Data = JsonSerializer.Serialize(data),
			Type = typeof(TData).Name
		};

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

		var (message, success) = await cn.DequeueAsync<TMessage>(QueueTableName, "[Type]=@type", new { type = typeof(TData).Name });

		if (success)
		{
			var data = JsonSerializer.Deserialize<TData>(message.Data);
			try
			{
				await DoWorkAsync(DateTime.UtcNow, message, data, stoppingToken);
			}
			catch (Exception exc)
			{
				Logger.LogError(exc, "Error in SqlServerQueueBackgroundService.DequeueAsync");
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
