using Dapper;
using Microsoft.Extensions.Hosting;
using System.Data;
using System.Text.Json;

namespace HostedService.Extensions;

/// <summary>
/// help from http://rusanu.com/2010/03/26/using-tables-as-queues/ Heap Queues
/// </summary>
public abstract class SqlServerQueueBackgroundService<T> : BackgroundService
{
	protected abstract IDbConnection GetConnection();

	protected abstract string QueueTableName { get; }

	protected abstract string ErrorTableName { get; }

	protected abstract Task DoWorkAsync(CancellationToken stoppingToken, T item);

	public async Task DequeueAsync(CancellationToken stoppingToken)
	{
		using var cn = GetConnection();

		T item = await cn.QuerySingleOrDefaultAsync<T>($"DELETE TOP (1) FROM {QueueTableName} WITH (ROWLOCK, READPAST) OUTPUT [deleted].*");

		if (item is not null)
		{
			try
			{
				await DoWorkAsync(stoppingToken, item);
			}
			catch (Exception exc)
			{
				await LogErrorAsync(exc, item);
			}
		}
	}

	private async Task LogErrorAsync(Exception exception, T item)
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
			await Task.Delay(1000); // a little breathing room
		}		
	}
}
