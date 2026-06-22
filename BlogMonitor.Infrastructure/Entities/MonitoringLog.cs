using System;
using System.ComponentModel.DataAnnotations;

namespace BlogMonitor.Infrastructure.Entities
{
    public class MonitoringLog
    {
        public int Id { get; set; }

        public int? ClientUrlId { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = null!;

        [Required]
        public string Message { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Level { get; set; } = null!;

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ClientUrl? ClientUrl { get; set; }
    }
}
