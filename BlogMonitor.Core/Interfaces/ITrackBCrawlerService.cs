using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;

namespace BlogMonitor.Core.Interfaces
{
    public interface ITrackBCrawlerService
    {
        Task<SitemapUrlList> CrawlHomepageAsync(string siteUrl);
    }
}
