using System.Threading.Tasks;
using System;

namespace BlogMonitor.Core.Interfaces
{
    public interface IWeeklyReportGeneratorService
    {
        Task GenerateAndStoreAsync(int clientUrlId, int clientId, int weekNumber, DateOnly cycleStartDate, DateOnly cycleEndDate);
    }
}
