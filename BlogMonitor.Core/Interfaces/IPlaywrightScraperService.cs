using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;

namespace BlogMonitor.Core.Interfaces
{
    public interface IPlaywrightScraperService
    {
        Task<ScrapedPage?> ScrapePageAsync(string url, int clientUrlId);
    }
}
