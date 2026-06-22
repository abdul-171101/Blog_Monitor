using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using BlogMonitor.Infrastructure.Data;
using BlogMonitor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlogMonitor.Infrastructure.Services
{
    public class DailyPipelineOrchestrator
    {
        private readonly ISitemapDiscoveryService _sitemapDiscovery;
        private readonly SitemapDownloaderService _sitemapDownloader;
        private readonly ITrackBCrawlerService _trackBCrawler;
        private readonly IPlaywrightScraperService _scraper;
        private readonly IContentExtractorService _extractor;
        private readonly IContentHashService _hasher;
        private readonly IMarkdownWriterService _mdWriter;
        private readonly ITfidfSummariserService _summariser;
        private readonly IDuckDbReportService _duckReport;
        private readonly IWeeklyCycleService _weeklyCycle;
        private readonly IWeeklyReportGeneratorService _weeklyGenerator;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<DailyPipelineOrchestrator> _logger;

        public DailyPipelineOrchestrator(
            ISitemapDiscoveryService sitemapDiscovery,
            SitemapDownloaderService sitemapDownloader,
            ITrackBCrawlerService trackBCrawler,
            IPlaywrightScraperService scraper,
            IContentExtractorService extractor,
            IContentHashService hasher,
            IMarkdownWriterService mdWriter,
            ITfidfSummariserService summariser,
            IDuckDbReportService duckReport,
            IWeeklyCycleService weeklyCycle,
            IWeeklyReportGeneratorService weeklyGenerator,
            ApplicationDbContext db,
            IConfiguration config,
            ILogger<DailyPipelineOrchestrator> logger)
        {
            _sitemapDiscovery = sitemapDiscovery;
            _sitemapDownloader = sitemapDownloader;
            _trackBCrawler = trackBCrawler;
            _scraper = scraper;
            _extractor = extractor;
            _hasher = hasher;
            _mdWriter = mdWriter;
            _summariser = summariser;
            _duckReport = duckReport;
            _weeklyCycle = weeklyCycle;
            _weeklyGenerator = weeklyGenerator;
            _db = db;
            _config = config;
            _logger = logger;
        }

        public async Task RunAsync(int clientUrlId)
        {
            try
            {
                var cu = await _db.ClientUrls.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == clientUrlId);
                if (cu == null)
                {
                    _logger.LogInformation("ClientUrl {Id} not found, skipping", clientUrlId);
                    return;
                }
                if (cu.Status == "STOPPED")
                {
                    _logger.LogInformation("ClientUrl {Id} is stopped, skipping", clientUrlId);
                    return;
                }

                // Phase 1: discovery
                SitemapUrlList urlList;
                var sitemap = await _sitemapDiscovery.DiscoverSitemapUrlAsync(cu.Url);
                if (!string.IsNullOrWhiteSpace(sitemap))
                {
                    cu.SitemapUrl = sitemap;
                    cu.TrackType = "SITEMAP";
                    await _db.SaveChangesAsync();
                    urlList = await _sitemapDownloader.DownloadAndParseAsync(sitemap, cu.Id, cu.ClientId);
                }
                else
                {
                    cu.TrackType = "HASH_BASED";
                    await _db.SaveChangesAsync();
                    urlList = await _trackBCrawler.CrawlHomepageAsync(cu.Url);
                }

                if (urlList == null || urlList.Urls == null || urlList.Urls.Count == 0)
                {
                    _logger.LogWarning("Discovery returned no URLs for ClientUrl {Id}", clientUrlId);
                    var emptyRecord = new DailySummaryRecord { ClientUrlId = clientUrlId, Date = DateOnly.FromDateTime(DateTime.UtcNow), Status = "NO_CHANGE", PagesChanged = 0, PagesNoChange = 0, PagesFailed = 0 };
                    await _duckReport.InsertDailySummaryAsync(emptyRecord);
                    return;
                }

                if (int.TryParse(_config["Scraper:MaxUrlsPerRun"], out var maxUrlsPerRun) && maxUrlsPerRun > 0 && urlList.Urls.Count > maxUrlsPerRun)
                {
                    _logger.LogInformation("Limiting URL discovery count from {Count} to {Max} for ClientUrl {Id}", urlList.Urls.Count, maxUrlsPerRun, clientUrlId);
                    urlList.Urls = urlList.Urls.Take(maxUrlsPerRun).ToList();
                }

                // Phase 2: per-URL
                int pagesChanged = 0, pagesNoChange = 0, pagesFailed = 0;
                var pageSummaries = new List<string>();
                int delaySec = int.TryParse(_config["Scraper:DelayBetweenPagesSeconds"], out var dsec) ? dsec : 2;

                foreach (var url in urlList.Urls)
                {
                    var scraped = await _scraper.ScrapePageAsync(url, clientUrlId);
                    if (scraped == null)
                    {
                        pagesFailed++;
                        continue;
                    }

                    var extracted = _extractor.ExtractContent(scraped.CleanText, url);
                    if (extracted == null)
                    {
                        pagesFailed++;
                        continue;
                    }

                    var newHash = _hasher.ComputeHash(extracted.CleanText);
                    var cmp = await _hasher.CompareAndGetResultAsync(clientUrlId, url, newHash);
                    if (!cmp.IsChanged)
                    {
                        pagesNoChange++;
                        continue;
                    }

                    pagesChanged++;
                    var mdPath = await _mdWriter.WriteMarkdownAsync(extracted, clientUrlId, cu.ClientId, cu.Id, DateOnly.FromDateTime(DateTime.UtcNow));
                    string? summaryText = _summariser.Summarise(mdPath);
                    string summaryStatus = summaryText == null ? "SUMMARY_FAILED" : "SUCCESS";
                    var wordCount = string.IsNullOrWhiteSpace(summaryText) ? 0 : summaryText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

                    await _duckReport.InsertPageSummaryAsync(clientUrlId, url, extracted.Title, DateOnly.FromDateTime(DateTime.UtcNow), cu.CycleWeekNumber == 0 ? null : cu.CycleWeekNumber, null, mdPath, summaryText ?? string.Empty, wordCount, summaryStatus);

                    await _hasher.UpsertHashAsync(clientUrlId, url, newHash);

                    if (!string.IsNullOrWhiteSpace(summaryText)) pageSummaries.Add(summaryText);

                    await Task.Delay(delaySec * 1000);
                }

                // Phase 3: daily summary
                string status;
                if (pagesFailed > 0) status = "PARTIAL";
                else if (pagesChanged > 0) status = "CHANGED";
                else status = "NO_CHANGE";

                // check initial run
                var existingCount = await _duckReport.GetDailySummaryAsync(clientUrlId, DateOnly.FromDateTime(DateTime.UtcNow));
                if (existingCount == null)
                {
                    status = "INITIAL_RUN";
                }

                var daily = new DailySummaryRecord
                {
                    ClientUrlId = clientUrlId,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow),
                    Status = status,
                    PagesChanged = pagesChanged,
                    PagesNoChange = pagesNoChange,
                    PagesFailed = pagesFailed,
                    PageSummaries = pageSummaries
                };

                await _duckReport.InsertDailySummaryAsync(daily);

                cu.LastRunAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // Phase 4: weekly check
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                if (_weeklyCycle.IsEndOfCycle(today))
                {
                    // generate weekly report
                    await _weeklyGenerator.GenerateAndStoreAsync(cu.Id, cu.ClientId, cu.CycleWeekNumber ?? 0, cu.CycleStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow), today);
                }

                // if it's Monday and cycle should begin (simplified check)
                if (today.DayOfWeek == DayOfWeek.Monday)
                {
                    var weekStart = cu.CycleStartDate;
                    if (weekStart.HasValue && weekStart.Value.AddDays(7).DayNumber <= today.DayNumber)
                    {
                        await _weeklyCycle.InitializeNewCycleAsync(cu.Id, today, (cu.CycleWeekNumber ?? 0) + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    _db.MonitoringLogs.Add(new MonitoringLog
                    {
                        ClientUrlId = clientUrlId > 0 ? clientUrlId : null,
                        EventType = "ERROR",
                        Level = "ERROR",
                        Message = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();
                }
                catch { }

                _logger.LogError(ex, "Daily pipeline failed for ClientUrl {Id}", clientUrlId);
                throw;
            }
        }
    }
}
