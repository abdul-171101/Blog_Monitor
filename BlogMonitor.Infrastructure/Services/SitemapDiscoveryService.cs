using System;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using BlogMonitor.Core.Interfaces;

using System.IO;

namespace BlogMonitor.Infrastructure.Services
{
    public class SitemapDiscoveryService : ISitemapDiscoveryService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SitemapDiscoveryService> _logger;
        private readonly HttpClient _http;

        public SitemapDiscoveryService(IConfiguration config, ILogger<SitemapDiscoveryService> logger)
        {
            _config = config;
            _logger = logger;
            _http = new HttpClient();
        }

        public async Task<string?> DiscoverSitemapUrlAsync(string siteUrl)
        {
            try
            {
                var robotsUri = new Uri(new Uri(siteUrl), "/robots.txt");
                try
                {
                    var txt = await _http.GetStringAsync(robotsUri);
                    using var sr = new StringReader(txt);
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.TrimStart().StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(':', 2);
                            if (parts.Length == 2)
                            {
                                var url = parts[1].Trim();
                                if (!string.IsNullOrEmpty(url)) return url;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch robots.txt for {Site}", siteUrl);
                }

                var candidates = new[] { "/sitemap.xml", "/sitemap_index.xml", "/sitemap/sitemap.xml", "/wp-sitemap.xml" };
                foreach (var path in candidates)
                {
                    try
                    {
                        var uri = new Uri(new Uri(siteUrl), path);
                        using var res = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
                        if (res.IsSuccessStatusCode) return uri.ToString();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "HEAD check failed for {Path}", path);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error discovering sitemap for {Site}", siteUrl);
                return null;
            }
        }
    }
}
