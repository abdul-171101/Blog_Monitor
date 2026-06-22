using System;

namespace BlogMonitor.Core.DTOs
{
    public record WeeklyReportListItem(long Id, int ClientUrlId, string Url, DateOnly? WeekStartDate, DateOnly? WeekEndDate, int? WeekNumber, string? CycleLabel, int TotalDays, int DaysChanged, int DaysNoChange, int TotalPagesScraped, string EmailStatus, DateTime CreatedAt);

    public record WeeklyReportDetailResponse(long Id, int ClientUrlId, string Url, DateOnly? WeekStartDate, DateOnly? WeekEndDate, int? WeekNumber, string? CycleLabel, string? ReportJson, string? MdFilePath, int TotalDays, int DaysChanged, int DaysNoChange, int TotalPagesScraped, string EmailStatus, DateTime? EmailSentAt);

    public record DailySummaryResponse(long Id, int ClientUrlId, DateOnly SummaryDate, int? DayInCycle, string? Status, int PagesChanged, int PagesNoChange, int PagesFailed, string? CompiledSummary, DateTime CreatedAt);

    public record DownloadReportRequest(long ReportId, string Format);
}
