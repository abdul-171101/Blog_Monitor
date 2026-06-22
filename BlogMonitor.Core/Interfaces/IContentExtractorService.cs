using BlogMonitor.Core.DTOs;

namespace BlogMonitor.Core.Interfaces
{
    public interface IContentExtractorService
    {
        ScrapedPage? ExtractContent(string rawHtml, string pageUrl);
    }
}
