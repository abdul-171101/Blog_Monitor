using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlogMonitor.Infrastructure.DuckDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlogMonitor.Infrastructure.Data
{
    public class DatabaseInitializer : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly DuckDbContext _duckDbContext;
        private readonly IServiceScopeFactory _scopeFactory;

        public DatabaseInitializer(IConfiguration configuration, DuckDbContext duckDbContext, IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _duckDbContext = duckDbContext ?? throw new ArgumentNullException(nameof(duckDbContext));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // 1) Create storage directories
            var basePath = _configuration.GetValue<string>("Storage:BasePath") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BlogMonitorStorage");
            var subdirs = new[] { "sitemaps", "markdown", "reports", "analytics", "logs" };
            foreach (var d in subdirs)
            {
                Directory.CreateDirectory(Path.Combine(basePath, d));
            }

            // 2) Execute DuckDB schema statements
            var conn = _duckDbContext.Connection;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = DuckDbSchema.CreatePageSummariesTable;
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = DuckDbSchema.CreateDailySummariesTable;
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = DuckDbSchema.CreateWeeklyReportsTable;
                cmd.ExecuteNonQuery();
            }

            // 3) Apply pending EF Core migrations
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.MigrateAsync(cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
