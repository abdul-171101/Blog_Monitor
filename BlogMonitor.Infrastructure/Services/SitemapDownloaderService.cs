using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using BlogMonitor.Infrastructure.Data;
using BlogMonitor.Infrastructure.Entities;

using System.Collections.Generic;

namespace BlogMonitor.Infrastructure.Services
{
    public class SitemapDownloaderService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SitemapDownloaderService> _logger;
        private readonly HttpClient _http;

        public SitemapDownloaderService(IConfiguration config, ApplicationDbContext db, ILogger<SitemapDownloaderService> logger)
        {
            _config = config;
            _db = db;
            _logger = logger;
            _http = new HttpClient();
        }

        public async Task<SitemapUrlList> DownloadAndParseAsync(string sitemapUrl, int clientUrlId, int clientId)
        {
            var result = new SitemapUrlList { TrackType = "SITEMAP", DiscoveryDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            var storageBase = _config.GetValue<string>("Storage:BasePath") ?? _config["StorageBasePath"] ?? "storage";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            try
            {
                var xml = await _http.GetStringAsync(sitemapUrl);
                // parse
                var doc = XDocument.Parse(xml);
                var urls = new HashSet<string>();

                async Task ParseDocAsync(XDocument d)
                {
                    var root = d.Root;
                    if (root == null) return;
                    var name = root.Name.LocalName.ToLowerInvariant();
                    if (name == "sitemapindex")
                    {
                        var locs = root.Descendants().Where(e => e.Name.LocalName.ToLowerInvariant() == "loc").Select(e => e.Value.Trim());
                        foreach (var loc in locs)
                        {
                            try
                            {
                                var childXml = await _http.GetStringAsync(loc);
                                var childDoc = XDocument.Parse(childXml);
                                var childLocs = childDoc.Root?.Descendants().Where(e => e.Name.LocalName.ToLowerInvariant() == "loc").Select(e => e.Value.Trim()) ?? Enumerable.Empty<string>();
                                foreach (var c in childLocs) urls.Add(c);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to fetch child sitemap {Loc}", loc);
                            }
                        }
                    }
                    else if (name == "urlset")
                    {
                        var locs = root.Descendants().Where(e => e.Name.LocalName.ToLowerInvariant() == "loc").Select(e => e.Value.Trim());
                        foreach (var l in locs) urls.Add(l);
                    }
                }

                await ParseDocAsync(doc);

                // save raw xml
                var dir = Path.Combine(storageBase, "sitemaps", clientId.ToString(), clientUrlId.ToString());
                Directory.CreateDirectory(dir);
                var filePath = Path.Combine(dir, $"{today:yyyy-MM-dd}.xml");
                await File.WriteAllTextAsync(filePath, xml);

                var sd = new SitemapDownload
                {
                    ClientUrlId = clientUrlId,
                    DownloadDate = today,
                    FilePath = filePath,
                    UrlCount = urls.Count,
                    DownloadStatus = "SUCCESS"
                };
                _db.SitemapDownloads.Add(sd);
                await _db.SaveChangesAsync();

                result.Urls = urls.ToList();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sitemap download failed for {Url}", sitemapUrl);
                try
                {
                    var sd = new SitemapDownload
                    {
                        ClientUrlId = clientUrlId,
                        DownloadDate = today,
                        FilePath = string.Empty,
                        UrlCount = 0,
                        DownloadStatus = "FAILED"
                    };
                    _db.SitemapDownloads.Add(sd);
                    await _db.SaveChangesAsync();
                }
                catch (Exception dbex)
                {
                    _logger.LogWarning(dbex, "Failed to write failed sitemap download record");
                }

                return result; // empty
            }
        }
    }
}
