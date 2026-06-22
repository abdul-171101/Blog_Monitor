using System;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlogMonitor.Infrastructure.Services
{
    public class ContentExtractorService : IContentExtractorService
    {
        private readonly ILogger<ContentExtractorService> _logger;

        public ContentExtractorService(ILogger<ContentExtractorService> logger)
        {
            _logger = logger;
        }

        public ScrapedPage? ExtractContent(string rawHtml, string pageUrl)
        {
            try
            {
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(rawHtml);

                // remove unwanted tags
                var removeTags = new[] { "script", "style", "noscript", "iframe", "form", "svg", "canvas", "header", "footer", "nav", "aside" };
                foreach (var t in removeTags)
                {
                    foreach (var el in doc.QuerySelectorAll(t).ToArray()) el.Remove();
                }

                // remove by class or id substrings
                var substrings = new[] { "ad", "ads", "advertisement", "banner", "promo", "sidebar", "cookie", "popup", "social", "share", "related", "newsletter", "subscribe" };
                var allElements = doc.All.ToArray();
                foreach (var el in allElements)
                {
                    var cls = el.GetAttribute("class") ?? string.Empty;
                    var id = el.GetAttribute("id") ?? string.Empty;
                    var combined = (cls + " " + id).ToLowerInvariant();
                    if (substrings.Any(s => combined.Contains(s))) el.Remove();
                }

                // find main container
                string[] selectors = new[] { "article", "main", "[role='main']", ".post-content", ".entry-content", ".article-body", "#content", "#main" };
                IElement? main = null;
                foreach (var sel in selectors)
                {
                    main = doc.QuerySelector(sel);
                    if (main != null) break;
                }
                if (main == null) main = doc.Body ?? doc.DocumentElement;

                var text = main.TextContent ?? string.Empty;
                var cleaned = Regex.Replace(text, "\\s+", " ").Trim();

                var title = doc.Title;
                if (string.IsNullOrWhiteSpace(title)) title = null;

                return new ScrapedPage { Url = pageUrl, Title = title, CleanText = cleaned, ScrapedAt = DateTime.UtcNow };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Content extraction failed for {Url}", pageUrl);
                return null;
            }
        }
    }
}
