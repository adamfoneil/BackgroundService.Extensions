using Dapper;
using Microsoft.Data.SqlClient;
using SqlServer.LocalDb;

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

        var queue = new DemoQueueProcessor(LocalDb.GetConnectionString(DbName));

		await queue.EnqueueAsync("test", "hello");

        await queue.DequeueAsync(new CancellationToken());
    }

    [TestMethod]
	public async Task QueueWithError()
	{
		using var cn = LocalDb.GetConnection(DbName);

		await InitObjectsAsync(cn);		

		var queue = new DemoQueueProcessor(LocalDb.GetConnectionString(DbName))
		{
			SimulateError = true
		};

		await queue.EnqueueAsync("test", "this will fail");

		await queue.DequeueAsync(new CancellationToken());
	}

	private static async Task InitObjectsAsync(SqlConnection cn)
	{
		await cn.ExecuteAsync(
			@"DROP TABLE IF EXISTS [dbo].[Queue];
			DROP TABLE IF EXISTS [dbo].[Error];

			CREATE TABLE [dbo].[Queue] (
				[Id] bigint identity(1,1) PRIMARY KEY,
				[UserName] nvarchar(50) NOT NULL,
				[Queued] datetime NOT NULL,
				[Data] nvarchar(max) NOT NULL
			);

			CREATE TABLE [dbo].[Error] (
				[Id] int identity(1,1) PRIMARY KEY,
				[Timestamp] datetime NOT NULL DEFAULT (getdate()),
				[Message] nvarchar(max) NOT NULL,
				[Data] nvarchar(max) NOT NULL
			);");
	}
}