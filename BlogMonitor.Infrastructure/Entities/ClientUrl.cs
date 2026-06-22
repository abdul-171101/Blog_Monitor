using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlogMonitor.Infrastructure.Entities
{
    public class ClientUrl
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        [MaxLength(2048)]
        public string Url { get; set; } = null!;

        [MaxLength(2048)]
        public string? SitemapUrl { get; set; }

        [Required]
        [MaxLength(20)]
        public string TrackType { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE";

        [Required]
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastRunAt { get; set; }

        public DateOnly? CycleStartDate { get; set; }

        public int? CycleWeekNumber { get; set; }

        public Client Client { get; set; } = null!;

        public ICollection<ContentHash> ContentHashes { get; set; } = new List<ContentHash>();

        public ICollection<SitemapDownload> SitemapDownloads { get; set; } = new List<SitemapDownload>();

        public ICollection<MonitoringLog> MonitoringLogs { get; set; } = new List<MonitoringLog>();
    }
}
