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

        var id = await queue.EnqueueAsync("test", "hello");
        Assert.IsTrue(id != 0);

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
			DROP TABLE IF EXISTS [dbo].[Error];");
        await cn.ExecuteAsync(DemoQueueProcessor.QueueTableSql("dbo.Queue"));
        await cn.ExecuteAsync(DemoQueueProcessor.ErrorTableSql("dbo.Error"));
    }
}