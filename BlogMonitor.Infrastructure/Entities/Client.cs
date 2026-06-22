using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlogMonitor.Infrastructure.Entities
{
    public class Client
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = null!;

        [MaxLength(200)]
        public string? CompanyName { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<ClientUrl> ClientUrls { get; set; } = new List<ClientUrl>();
    }
}
