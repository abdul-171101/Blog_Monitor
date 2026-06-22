using System.Collections.Generic;
using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;
using System;

namespace BlogMonitor.Core.Interfaces
{
    public interface IReportQueryService
    {
        Task<List<WeeklyReportListItem>> GetWeeklyReportsAsync(int clientUrlId);
        Task<WeeklyReportDetailResponse?> GetWeeklyReportDetailAsync(long reportId);
        Task<DailySummaryResponse?> GetDailySummaryAsync(int clientUrlId, DateOnly date);
        Task ResendEmailAsync(long reportId);
    }
}
