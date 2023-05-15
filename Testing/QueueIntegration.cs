using Dapper;
using Microsoft.Data.SqlClient;
using SqlServer.LocalDb;
using Testing.Models;

namespace Testing;

[TestClass]
public class QueueIntegration
{
	const string DbName = "QueueDemo";

	[TestMethod]
	public async Task SimpleQueueExample()
	{	
		using var cn = LocalDb.GetConnection(DbName);

		await InitObjectsAsync(cn);

		await cn.ExecuteAsync(
			"INSERT INTO [dbo].[Queue] ([Timestamp], [Message]) VALUES (@timestamp, @message)",
			new QueueItem()
			{
				Timestamp = DateTime.Now,
				Message = "Hello"
			});

		var queue = new DemoQueueProcessor(LocalDb.GetConnectionString(DbName));
		await queue.DequeueAsync(new CancellationToken());
	}

	[TestMethod]
	public async Task QueueWithError()
	{
		using var cn = LocalDb.GetConnection(DbName);

		await InitObjectsAsync(cn);

		await cn.ExecuteAsync(
			"INSERT INTO [dbo].[Queue] ([Timestamp], [Message]) VALUES (@timestamp, @message)",
			new QueueItem()
			{
				Timestamp = DateTime.Now,
				Message = "This will fail"
			});

		var queue = new DemoQueueProcessor(LocalDb.GetConnectionString(DbName))
		{
			Throw = true
		};

		await queue.DequeueAsync(new CancellationToken());
	}

	private static async Task InitObjectsAsync(SqlConnection cn)
	{
		await cn.ExecuteAsync(
			@"DROP TABLE IF EXISTS [dbo].[Queue];
			DROP TABLE IF EXISTS [dbo].[Error];

			CREATE TABLE [dbo].[Queue] (
				[Id] int identity(1,1) PRIMARY KEY,
				[Timestamp] datetime NOT NULL,
				[Message] nvarchar(max) NOT NULL
			);

			CREATE TABLE [dbo].[Error] (
				[Id] int identity(1,1) PRIMARY KEY,
				[Timestamp] datetime NOT NULL DEFAULT (getdate()),
				[Message] nvarchar(max) NOT NULL,
				[Data] nvarchar(max) NOT NULL
			);");
	}
}