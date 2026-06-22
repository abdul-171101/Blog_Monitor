using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using BlogMonitor.Infrastructure.Data;
using Hangfire;
using BlogMonitor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogMonitor.Infrastructure.Services
{
    public class ClientUrlService : IClientUrlService
    {
        private readonly ApplicationDbContext _db;
        private readonly Hangfire.IBackgroundJobClient _backgroundJobClient;
        private readonly Hangfire.IRecurringJobManager _recurringJobManager;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public ClientUrlService(ApplicationDbContext db, Hangfire.IBackgroundJobClient backgroundJobClient, Hangfire.IRecurringJobManager recurringJobManager, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _config = config;
        }

        public async Task<List<UrlSummaryResponse>> GetAllUrlsAsync()
        {
            var urls = await _db.ClientUrls.Include(u => u.Client).OrderByDescending(u => u.RegisteredAt).ToListAsync();

            return urls.Select(u => new UrlSummaryResponse(
                u.Id,
                u.ClientId,
                u.Client.FullName,
                u.Url,
                u.TrackType,
                u.Status,
                u.RegisteredAt,
                u.LastRunAt,
                u.CycleWeekNumber,
                null
            )).ToList();
        }

        public async Task<UrlDetailResponse?> GetUrlByIdAsync(int id)
        {
            var u = await _db.ClientUrls.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return null;

            return new UrlDetailResponse(
                u.Id,
                u.ClientId,
                u.Client.FullName,
                u.Url,
                u.SitemapUrl,
                u.TrackType,
                u.Status,
                u.RegisteredAt,
                u.LastRunAt,
                u.CycleStartDate,
                u.CycleWeekNumber
            );
        }

        public async Task<UrlDetailResponse> RegisterUrlAsync(RegisterUrlRequest request)
        {
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var _))
            {
                throw new ArgumentException("Invalid URL format.");
            }

            var exists = await _db.ClientUrls.AnyAsync(u => u.ClientId == request.ClientId && u.Url == request.Url);
            if (exists) throw new InvalidOperationException("This URL is already registered for this client.");

            var now = DateTime.UtcNow;
            var url = new ClientUrl
            {
                ClientId = request.ClientId,
                Url = request.Url,
                Status = "ACTIVE",
                RegisteredAt = now,
                TrackType = "SITEMAP",
                CycleStartDate = DateOnly.FromDateTime(now)
            };

            _db.ClientUrls.Add(url);
            await _db.SaveChangesAsync();

            // Enqueue immediate run (use static helper)
            BackgroundJob.Enqueue<BlogMonitor.Infrastructure.Jobs.DailyPipelineJob>(j => j.ExecuteAsync(url.Id));

            // Create recurring job
            var cron = _config["Scheduler:DailyCron"] ?? "0 2 * * *";
            var jobId = $"pipeline-{url.Id}";
            RecurringJob.AddOrUpdate<BlogMonitor.Infrastructure.Jobs.DailyPipelineJob>(jobId, j => j.ExecuteAsync(url.Id), cron, TimeZoneInfo.Local);

            var client = await _db.Clients.FindAsync(request.ClientId);

            return new UrlDetailResponse(url.Id, url.ClientId, client?.FullName ?? string.Empty, url.Url, url.SitemapUrl, url.TrackType, url.Status, url.RegisteredAt, url.LastRunAt, url.CycleStartDate, url.CycleWeekNumber);
        }

        public async Task<bool> StopMonitoringAsync(int id)
        {
            var u = await _db.ClientUrls.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return false;

            u.Status = "STOPPED";
            await _db.SaveChangesAsync();

            var jobId = $"pipeline-{id}";
            _recurringJobManager.RemoveIfExists(jobId);

            return true;
        }

        public async Task<bool> TriggerManualRunAsync(int id)
        {
            var u = await _db.ClientUrls.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return false;
            if (u.Status == "STOPPED") return false;

            _backgroundJobClient.Enqueue<BlogMonitor.Infrastructure.Jobs.DailyPipelineJob>(j => j.ExecuteAsync(id));

            return true;
        }
    }
}
