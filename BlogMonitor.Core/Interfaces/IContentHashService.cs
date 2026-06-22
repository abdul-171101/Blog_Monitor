using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;

namespace BlogMonitor.Core.Interfaces
{
    public interface IContentHashService
    {
        string ComputeHash(string cleanText);
        Task<HashComparisonResult> CompareAndGetResultAsync(int clientUrlId, string pageUrl, string newHash);
        Task UpsertHashAsync(int clientUrlId, string pageUrl, string newHash);
    }
}
