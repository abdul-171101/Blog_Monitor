using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using BlogMonitor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlogMonitor.Infrastructure.Services
{
    public class ReportQueryService : IReportQueryService
    {
        private readonly IDuckDbReportService _duck;
        private readonly IOutlookEmailSenderService _emailSender;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ReportQueryService> _logger;

        public ReportQueryService(IDuckDbReportService duck, IOutlookEmailSenderService emailSender, ApplicationDbContext db, ILogger<ReportQueryService> logger)
        {
            _duck = duck;
            _emailSender = emailSender;
            _db = db;
            _logger = logger;
        }

        public Task<List<WeeklyReportListItem>> GetWeeklyReportsAsync(int clientUrlId)
        {
            return _duck.GetWeeklyReportsAsync(clientUrlId);
        }

        public Task<WeeklyReportDetailResponse?> GetWeeklyReportDetailAsync(long reportId)
        {
            return _duck.GetWeeklyReportDetailAsync(reportId);
        }

        public Task<DailySummaryResponse?> GetDailySummaryAsync(int clientUrlId, DateOnly date)
        {
            return _duck.GetDailySummaryAsync(clientUrlId, date);
        }

        public async Task ResendEmailAsync(long reportId)
        {
            var report = await _duck.GetWeeklyReportDetailAsync(reportId);
            if (report == null)
            {
                _logger.LogWarning("Attempted to resend email for missing report {Id}", reportId);
                return;
            }

            var cu = await _db.ClientUrls.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == report.ClientUrlId);
            if (cu == null)
            {
                _logger.LogWarning("ClientUrl {Id} not found while resending report {ReportId}", report.ClientUrlId, reportId);
                return;
            }

            await _emailSender.SendWeeklyReportAsync(cu.Client?.Email ?? string.Empty, cu.Client?.FullName ?? string.Empty, reportId);
        }
    }
}
