using System.Collections.Generic;
using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;

namespace BlogMonitor.Core.Interfaces
{
    public interface IClientUrlService
    {
        Task<List<UrlSummaryResponse>> GetAllUrlsAsync();
        Task<UrlDetailResponse?> GetUrlByIdAsync(int id);
        Task<UrlDetailResponse> RegisterUrlAsync(RegisterUrlRequest request);
        Task<bool> StopMonitoringAsync(int id);
        Task<bool> TriggerManualRunAsync(int id);
    }
}
