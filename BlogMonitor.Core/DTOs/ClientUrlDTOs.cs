using System;

namespace BlogMonitor.Core.DTOs
{
    public record RegisterUrlRequest(int ClientId, string Url);

    public record UrlSummaryResponse(int Id, int ClientId, string ClientName, string Url, string TrackType, string Status, DateTime RegisteredAt, DateTime? LastRunAt, int? CycleWeekNumber, int? CycleDayNumber);

    public record UrlDetailResponse(int Id, int ClientId, string ClientName, string Url, string? SitemapUrl, string TrackType, string Status, DateTime RegisteredAt, DateTime? LastRunAt, DateOnly? CycleStartDate, int? CycleWeekNumber);
}
