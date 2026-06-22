using System;
using System.Threading.Tasks;
using System;
using System.Threading;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BlogMonitor.Infrastructure.Data;
using BlogMonitor.Infrastructure.Entities;
using Microsoft.Playwright;

using System.Text;

namespace BlogMonitor.Infrastructure.Services
{
    public class PlaywrightScraperService : IPlaywrightScraperService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<PlaywrightScraperService> _logger;
        private readonly ApplicationDbContext _db;

        public PlaywrightScraperService(IConfiguration config, ApplicationDbContext db, ILogger<PlaywrightScraperService> logger)
        {
            _config = config;
            _db = db;
            _logger = logger;
        }

        public async Task<ScrapedPage?> ScrapePageAsync(string url, int clientUrlId)
        {
            int maxRetries = int.TryParse(_config["Scraper:MaxRetries"], out var mr) ? mr : 3;
            int delaySec = int.TryParse(_config["Scraper:DelayBetweenPagesSeconds"], out var ds) ? ds : 1;
            int timeoutSec = int.TryParse(_config["Scraper:PageTimeoutSeconds"], out var ts) ? ts : 60;

            for (int attempt = 1; attempt <= Math.Max(1, maxRetries); attempt++)
            {
                try
                {
                    using var playwright = await Playwright.CreateAsync();
                    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                    var context = await browser.NewContextAsync(new BrowserNewContextOptions { UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36" });
                    var page = await context.NewPageAsync();
                    await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = timeoutSec * 1000 });
                    var title = await page.TitleAsync();
                    var html = await page.ContentAsync();
                    await page.CloseAsync();
                    return new ScrapedPage { Url = url, Title = title, CleanText = html, ScrapedAt = DateTime.UtcNow };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Scrape attempt {Attempt} failed for {Url}", attempt, url);
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(delaySec * 1000);
                        continue;
                    }
                    // log monitoring record
                    try
                    {
                        _db.MonitoringLogs.Add(new MonitoringLog
                        {
                            ClientUrlId = clientUrlId,
                            EventType = "SCRAPE",
                            Level = "WARNING",
                            Message = $"Scrape failed for {url}: {ex.Message}",
                            Timestamp = DateTime.UtcNow
                        });
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception dbex)
                    {
                        _logger.LogWarning(dbex, "Failed to write monitoring log for scrape failure");
                    }

                    return null;
                }
            }

            return null;
        }
    }
}
