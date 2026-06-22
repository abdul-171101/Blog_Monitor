using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;

namespace BlogMonitor.Core.Interfaces
{
    public interface IDuckDbReportService
    {
        Task InsertPageSummaryAsync(int clientUrlId, string pageUrl, string? pageTitle, DateOnly summaryDate, int? weekNumber, int? cycleDayNumber, string mdFilePath, string summaryText, int wordCount, string summaryStatus);
        Task InsertDailySummaryAsync(DailySummaryRecord record);
        Task InsertWeeklyReportAsync(int clientUrlId, int clientId, DateOnly weekStartDate, DateOnly weekEndDate, int weekNumber, string cycleLabel, string reportJson, string mdFilePath, string pdfFilePath, string docxFilePath, int totalDays, int daysChanged, int daysNoChange, int totalPagesScraped);
        Task UpdateEmailStatusAsync(long reportId, string emailStatus, DateTime? emailSentAt);
        Task<List<WeeklyReportListItem>> GetWeeklyReportsAsync(int clientUrlId);
        Task<WeeklyReportDetailResponse?> GetWeeklyReportDetailAsync(long reportId);
        Task<DailySummaryResponse?> GetDailySummaryAsync(int clientUrlId, DateOnly date);
    }
}
