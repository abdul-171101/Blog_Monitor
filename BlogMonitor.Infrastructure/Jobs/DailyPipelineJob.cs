using System;
using System.Threading.Tasks;
using BlogMonitor.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace BlogMonitor.Infrastructure.Jobs
{
    public class DailyPipelineJob
    {
        private readonly DailyPipelineOrchestrator _orchestrator;
        private readonly ILogger<DailyPipelineJob> _logger;

        public DailyPipelineJob(DailyPipelineOrchestrator orchestrator, ILogger<DailyPipelineJob> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        public async Task ExecuteAsync(int clientUrlId)
        {
            _logger.LogInformation("Starting pipeline job for ClientUrl {Id}", clientUrlId);
            try
            {
                await _orchestrator.RunAsync(clientUrlId);
                _logger.LogInformation("Completed pipeline job for ClientUrl {Id}", clientUrlId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pipeline job failed for ClientUrl {Id}", clientUrlId);
                throw;
            }
        }
    }
}
