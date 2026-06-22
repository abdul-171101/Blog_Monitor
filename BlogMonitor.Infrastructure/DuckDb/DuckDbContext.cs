using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using DuckDB.NET.Data;

namespace BlogMonitor.Infrastructure.DuckDb
{
    public class DuckDbContext : IDisposable
    {
        private readonly IConfiguration _configuration;
        private DuckDBConnection? _connection;
        private readonly object _lock = new object();
        private readonly string _filePath;

        public DuckDbContext(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            var basePath = _configuration.GetValue<string>("Storage:BasePath") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BlogMonitorStorage");
            _filePath = Path.Combine(basePath, "analytics", "blogmonitor.duckdb");
        }

        public DuckDBConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    lock (_lock)
                    {
                        if (_connection == null)
                        {
                            var dir = Path.GetDirectoryName(_filePath)!;
                            Directory.CreateDirectory(dir);
                            // DataSource is the accepted connection string key for DuckDB.NET
                            _connection = new DuckDBConnection($"DataSource={_filePath}");
                            _connection.Open();
                        }
                    }
                }

                return _connection!;
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
