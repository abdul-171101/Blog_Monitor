using System;
using System.Collections.Generic;

namespace BlogMonitor.Core.DTOs
{
    public class SitemapUrlList
    {
        public List<string> Urls { get; set; } = new List<string>();
        public string TrackType { get; set; } = null!;
        public DateOnly DiscoveryDate { get; set; }
    }

    public class ScrapedPage
    {
        public string Url { get; set; } = null!;
        public string? Title { get; set; }
        public string CleanText { get; set; } = null!;
        public DateTime ScrapedAt { get; set; }
    }

    public class HashComparisonResult
    {
        public string PageUrl { get; set; } = null!;
        public string? StoredHash { get; set; }
        public string NewHash { get; set; } = null!;
        public bool IsChanged { get; set; }
    }

    public class DailySummaryRecord
    {
        public int ClientUrlId { get; set; }
        public DateOnly Date { get; set; }
        public string Status { get; set; } = null!;
        public int PagesChanged { get; set; }
        public int PagesNoChange { get; set; }
        public int PagesFailed { get; set; }
        public List<string> PageSummaries { get; set; } = new List<string>();
    }
}
