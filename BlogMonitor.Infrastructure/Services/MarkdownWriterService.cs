using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace BlogMonitor.Infrastructure.Services
{
    public class MarkdownWriterService : IMarkdownWriterService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MarkdownWriterService> _logger;

        public MarkdownWriterService(IConfiguration config, ILogger<MarkdownWriterService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<string> WriteMarkdownAsync(ScrapedPage page, int clientUrlId, int clientId, int urlId, DateOnly date)
        {
            try
            {
                var storageBase = _config.GetValue<string>("Storage:BasePath") ?? _config["StorageBasePath"] ?? "storage";
                var uri = new Uri(page.Url);
                var path = uri.AbsolutePath;
                var slug = Regex.Replace(path, "[^a-z0-9]", "-", RegexOptions.IgnoreCase).ToLowerInvariant();
                slug = Regex.Replace(slug, "-+", "-").Trim('-');
                if (slug.Length > 100) slug = slug[..100];
                if (string.IsNullOrWhiteSpace(slug)) slug = "page";

                var dir = Path.Combine(storageBase, "markdown", clientId.ToString(), urlId.ToString(), date.ToString("yyyy-MM-dd"));
                Directory.CreateDirectory(dir);
                var filePath = Path.Combine(dir, slug + ".md");

                var title = string.IsNullOrWhiteSpace(page.Title) ? "Untitled" : page.Title;
                var scrapedAt = page.ScrapedAt.ToString("yyyy-MM-dd HH:mm:ss UTC");

                var content = $"# {title}\n\nSource: {page.Url}\nScraped: {scrapedAt}\n---\n\n{page.CleanText}";

                await File.WriteAllTextAsync(filePath, content, System.Text.Encoding.UTF8);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write markdown for {Url}", page.Url);
                throw;
            }
        }
    }
}
