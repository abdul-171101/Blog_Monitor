using System;
using System.ComponentModel.DataAnnotations;

namespace BlogMonitor.Infrastructure.Entities
{
    public class SitemapDownload
    {
        public int Id { get; set; }

        [Required]
        public int ClientUrlId { get; set; }

        [Required]
        public DateOnly DownloadDate { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = null!;

        public int UrlCount { get; set; }

        [Required]
        [MaxLength(20)]
        public string DownloadStatus { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ClientUrl ClientUrl { get; set; } = null!;
    }
}
