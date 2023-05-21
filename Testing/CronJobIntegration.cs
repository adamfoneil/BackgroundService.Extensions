using Dapper;
using Microsoft.Data.SqlClient;
using SqlServer.LocalDb;

namespace Testing;

[TestClass]
public class CronJobIntegration
{
    [TestMethod]
    public async Task SimpleJob()
    {
        using var cn = LocalDb.GetConnection(QueueIntegration.DbName);
        await InitObjectsAsync(cn);

        var job = new DemoCronJob(LocalDb.GetConnectionString(QueueIntegration.DbName));
        await job.RunManualAsync(new CancellationToken());
    }

    private async Task InitObjectsAsync(SqlConnection cn)
    {
        await cn.ExecuteAsync(
            @"DROP TABLE IF EXISTS [dbo].[CronJobInfo];
            DROP SEQUENCE IF EXISTS [dbo].[seq_CronJobExecution];");

        await cn.ExecuteAsync(DemoCronJob.TableSql("dbo.CronJobInfo"));
        await cn.ExecuteAsync(DemoCronJob.SequenceSql("dbo.seq_CronJobExecution"));
    }
}
