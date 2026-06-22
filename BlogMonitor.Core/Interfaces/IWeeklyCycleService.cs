using System;
using System.Threading.Tasks;

namespace BlogMonitor.Core.Interfaces
{
    public interface IWeeklyCycleService
    {
        int GetCycleDay(DateOnly registrationDate, DateOnly today);
        bool IsEndOfCycle(DateOnly today);
        string GetCycleLabel(int weekNumber, bool isPartial);
        Task InitializeNewCycleAsync(int clientUrlId, DateOnly newCycleStartDate, int newWeekNumber);
    }
}
