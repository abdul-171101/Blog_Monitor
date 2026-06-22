using System;
using System.Threading.Tasks;
using BlogMonitor.Infrastructure.Data;
using BlogMonitor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BlogMonitor.Tests.SmokeTests
{
    public class SqlServerSmokeTest
    {
        [Fact]
        public async Task InsertAndReadClient_ShouldSucceed()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets("b4b28f17-c4f6-4889-9f40-d2dd1ba72ad5")
                .Build();

            var connectionString = config["ConnectionStrings:SqlServer"];
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            await using var context = new ApplicationDbContext(options);

            var client = new Client
            {
                FullName = "Smoke Test Client",
                Email = "smoketest@test.com",
                Status = "ACTIVE"
            };

            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var fetched = await context.Clients.FirstOrDefaultAsync(c => c.Email == "smoketest@test.com");
            Assert.NotNull(fetched);
            Assert.Equal("Smoke Test Client", fetched!.FullName);

            context.Clients.Remove(fetched!);
            await context.SaveChangesAsync();
        }
    }
}
