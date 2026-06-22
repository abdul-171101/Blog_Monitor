using System.Threading.Tasks;

namespace BlogMonitor.Core.Interfaces
{
    public interface ISitemapDiscoveryService
    {
        Task<string?> DiscoverSitemapUrlAsync(string siteUrl);
    }
}
