using System;
using System.ComponentModel.DataAnnotations;

namespace BlogMonitor.Infrastructure.Entities
{
    public class ContentHash
    {
        public int Id { get; set; }

        [Required]
        public int ClientUrlId { get; set; }

        [Required]
        [MaxLength(2048)]
        public string PageUrl { get; set; } = null!;

        [Required]
        [MaxLength(64)]
        [System.ComponentModel.DataAnnotations.Schema.Column("ContentHash")]
        public string ContentHashValue { get; set; } = null!;

        [Required]
        public DateOnly LastHashDate { get; set; }

        public ClientUrl ClientUrl { get; set; } = null!;
    }
}
