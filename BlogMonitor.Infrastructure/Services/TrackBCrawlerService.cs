using System;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Linq;
using System.Collections.Generic;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AngleSharp.Html.Parser;

namespace BlogMonitor.Infrastructure.Services
{
    public class TrackBCrawlerService : ITrackBCrawlerService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TrackBCrawlerService> _logger;
        private readonly HttpClient _http;

        public TrackBCrawlerService(IConfiguration config, ILogger<TrackBCrawlerService> logger)
        {
            _config = config;
            _logger = logger;
            _http = new HttpClient();
        }

        public async Task<SitemapUrlList> CrawlHomepageAsync(string siteUrl)
        {
            var result = new SitemapUrlList { TrackType = "HASH_BASED", DiscoveryDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            try
            {
                var html = await _http.GetStringAsync(siteUrl);
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(html);
                var anchors = doc.QuerySelectorAll("a").OfType<AngleSharp.Dom.IElement>();
                var baseUri = new Uri(siteUrl);
                var filters = (_config["Scraper:TrackBPathFilter"] ?? "/blog,/article,/post,/news").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                int cap = int.TryParse(_config["Scraper:TrackBMaxUrls"], out var c) ? c : 20;

                var urls = new List<string>();
                foreach (var a in anchors)
                {
                    var href = a.GetAttribute("href");
                    if (string.IsNullOrWhiteSpace(href)) continue;
                    try
                    {
                        Uri u;
                        if (Uri.TryCreate(href, UriKind.Absolute, out u) == false)
                        {
                            u = new Uri(baseUri, href);
                        }
                        if (!string.Equals(u.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase)) continue;
                        if (!filters.Any(f => u.AbsolutePath.Contains(f, StringComparison.OrdinalIgnoreCase))) continue;
                        urls.Add(u.ToString());
                    }
                    catch { continue; }
                }

                var dedup = urls.Distinct().Take(cap).ToList();
                result.Urls = dedup;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TrackB crawl failed for {Site}", siteUrl);
                return result;
            }
        }
    }
}
