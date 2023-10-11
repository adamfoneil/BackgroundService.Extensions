using BackgroundServiceExtensions.Extensions;
using Dapper;
using Dommel;
using SqlServer.LocalDb;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Testing;

[TestClass]
public class QueueExtensions
{
	[TestMethod]
	public async Task ProcessQueue()
	{
		using var cn = LocalDb.GetConnection(QueueIntegration.DbName);

		await cn.ExecuteAsync(
			@"DROP TABLE IF EXISTS [dbo].[SampleQueue];
			CREATE TABLE [dbo].[SampleQueue] (
				[Id] int identity(1,1) PRIMARY KEY,
				[FirstName] nvarchar(50) NOT NULL,
				[Lastname] nvarchar(50) NOT NULL
			)");

		var items = new SampleQueueItem[]
		{
			new() { FirstName = "Wally", LastName = "World"},
			new() { FirstName = "Ender's", LastName = "Game" },
			new() { FirstName = "Gary", LastName = "Fisher" },
			new() { FirstName = "Darth", LastName = "Vader" }
		};

		await cn.InsertAllAsync(items);

		await cn.ProcessQueueAsync<SampleQueueItem>("dbo.SampleQueue", async (item) =>
		{
			Debug.Print(item.ToString());
			await Task.CompletedTask;
		}, new CancellationToken());
	}
}

[Table("SampleQueue")]
public class SampleQueueItem
{
	public int Id { get; set; }
	public string FirstName { get; set; } = default!;
	public string LastName { get; set; } = default!;

	public override string ToString() => $"Id = {Id}, FirstName = {FirstName}, LastName = {LastName}";	
}