using System;
using System.Data;
using System.Threading.Tasks;
using BlogMonitor.Infrastructure.DuckDb;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BlogMonitor.Tests.SmokeTests
{
    public class DuckDbSmokeTest
    {
        [Fact]
        public async Task InsertAndReadDailySummary_ShouldSucceed()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets("b4b28f17-c4f6-4889-9f40-d2dd1ba72ad5")
                .Build();

            using var ctx = new DuckDbContext(config);
            var conn = ctx.Connection;

            await Task.Run(() =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = DuckDbSchema.CreateDailySummariesTable.TrimEnd(';');
                cmd.ExecuteNonQuery();

                using var insert = conn.CreateCommand();
                insert.CommandText = $"INSERT INTO daily_summaries (client_url_id, summary_date, status, pages_changed, pages_no_change, pages_failed) VALUES (999, '{DateTime.Today:yyyy-MM-dd}', 'NO_CHANGE', 0, 5, 0)";
                insert.ExecuteNonQuery();

                using var query = conn.CreateCommand();
                query.CommandText = $"SELECT status, pages_no_change FROM daily_summaries WHERE client_url_id = 999 AND summary_date = '{DateTime.Today:yyyy-MM-dd}'";

                using var reader = query.ExecuteReader();
                if (!reader.Read()) throw new Exception("Inserted row not found");

                var status = reader.GetString(0);
                var pagesNoChange = reader.GetInt32(1);

                if (status != "NO_CHANGE") throw new Exception($"Unexpected status: {status}");
                if (pagesNoChange != 5) throw new Exception($"Unexpected pages_no_change: {pagesNoChange}");

                using var del = conn.CreateCommand();
                del.CommandText = "DELETE FROM daily_summaries WHERE client_url_id = 999";
                del.ExecuteNonQuery();
            });
            ctx.Dispose();
        }
    }
}
