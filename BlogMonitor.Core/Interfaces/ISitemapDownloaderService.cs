using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;

namespace BlogMonitor.Core.Interfaces
{
    public interface ISitemapDownloaderService
    {
        Task<SitemapUrlList> DownloadAndParseAsync(string sitemapUrl, int clientUrlId, int clientId);
    }
}
