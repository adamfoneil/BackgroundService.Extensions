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

	protected abstract Task<long> EnqueueInternalAsync(TMessage message);

	public async Task<long> EnqueueAsync(string userName, TData data)
	{
        ArgumentNullException.ThrowIfNull(userName, nameof(userName));
        ArgumentNullException.ThrowIfNull(data, nameof(data));
		
		var message = new TMessage();
		message.UserName = userName;
		message.Queued = DateTime.UtcNow;
		message.Data = JsonSerializer.Serialize(data);
		message.Type = typeof(TData).Name;
		return await EnqueueInternalAsync(message);
	}

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
				await LogErrorAsync(exc, message);
			}
		}
	}

	private async Task LogErrorAsync(Exception exception, TMessage item)
	{
		using var cn = GetConnection();

		try
		{
			await cn.ExecuteAsync($"INSERT INTO {ErrorTableName} ([Message], [Data]) VALUES (@message, @data)", new
			{
				message = exception.Message,
				data = JsonSerializer.Serialize(item)
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
