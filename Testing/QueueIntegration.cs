using Dapper;
using Microsoft.Data.SqlClient;
using SqlServer.LocalDb;

namespace Testing;

[TestClass]
public class QueueIntegration
{
    public const string DbName = "QueueDemo";

    [TestMethod]
    public async Task SimpleQueueExample()
    {
        using var cn = LocalDb.GetConnection(DbName);

        await InitObjectsAsync(cn);

        var queue = new DemoQueueProcessor(LocalDb.GetConnectionString(DbName));

        var id = await queue.EnqueueAsync("test", "hello");
        Assert.IsTrue(id != 0);

        // in a real app, the Dequeue would be called by the background service infrastructure, not your code.
        // This is just to prove that the SQL works for popping queue messages, parsing the json, and calling your worker method
        await queue.DequeueAsync(new CancellationToken());
    }

    [TestMethod]
    public async Task QueueWithError()
    {
        using var cn = LocalDb.GetConnection(DbName);

        await InitObjectsAsync(cn);

        var queue = new DemoQueueProcessor(LocalDb.GetConnectionString(DbName))
        {
            // in a real app, you would never need to "simulate errors" (probably), so don't 
            // follow this example in your app. This is merely to trigger the error internal error logging behavior
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