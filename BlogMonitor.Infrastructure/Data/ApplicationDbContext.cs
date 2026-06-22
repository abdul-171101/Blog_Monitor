using BlogMonitor.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogMonitor.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<ClientUrl> ClientUrls { get; set; } = null!;
        public DbSet<ContentHash> ContentHashes { get; set; } = null!;
        public DbSet<SitemapDownload> SitemapDownloads { get; set; } = null!;
        public DbSet<MonitoringLog> MonitoringLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Client>(entity =>
            {
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.CompanyName).HasMaxLength(200);
                entity.Property(e => e.Phone).HasMaxLength(30);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("ACTIVE");
                entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<ClientUrl>(entity =>
            {
                entity.Property(e => e.Url).IsRequired().HasMaxLength(2048);
                entity.Property(e => e.SitemapUrl).HasMaxLength(2048);
                entity.Property(e => e.TrackType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("ACTIVE");
                entity.Property(e => e.RegisteredAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => new { e.ClientId, e.Url }).IsUnique().HasDatabaseName("UQ_ClientUrls_ClientId_Url");
            });

            modelBuilder.Entity<ContentHash>(entity =>
            {
                entity.Property(e => e.PageUrl).IsRequired().HasMaxLength(2048);
                entity.Property(e => e.ContentHashValue).IsRequired().HasMaxLength(64).HasColumnName("ContentHash");
                entity.Property(e => e.LastHashDate).IsRequired();
                entity.HasIndex(e => new { e.ClientUrlId, e.PageUrl }).IsUnique().HasDatabaseName("UQ_ContentHashes_ClientUrlId_PageUrl");
            });

            modelBuilder.Entity<SitemapDownload>(entity =>
            {
                entity.Property(e => e.DownloadDate).IsRequired();
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.DownloadStatus).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<MonitoringLog>(entity =>
            {
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Level).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Timestamp).IsRequired().HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(e => e.ClientUrl)
                    .WithMany(c => c.MonitoringLogs)
                    .HasForeignKey(e => e.ClientUrlId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
