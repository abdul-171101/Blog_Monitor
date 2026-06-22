using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;

namespace BlogMonitor.Core.Interfaces
{
    public interface IMarkdownWriterService
    {
        Task<string> WriteMarkdownAsync(ScrapedPage page, int clientUrlId, int clientId, int urlId, DateOnly date);
    }
}
