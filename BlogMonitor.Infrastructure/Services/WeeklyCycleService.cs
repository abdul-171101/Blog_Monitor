using System;
using System.Threading.Tasks;
using BlogMonitor.Core.Interfaces;
using BlogMonitor.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System;


namespace BlogMonitor.Infrastructure.Services
{
    public class WeeklyCycleService : IWeeklyCycleService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<WeeklyCycleService> _logger;

        public WeeklyCycleService(ApplicationDbContext db, ILogger<WeeklyCycleService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public int GetCycleDay(DateOnly registrationDate, DateOnly today)
        {
            var diff = today.DayNumber - registrationDate.DayNumber + 1;
            return Math.Max(1, diff);
        }

        public bool IsEndOfCycle(DateOnly today)
        {
            return today.DayOfWeek == DayOfWeek.Sunday;
        }

        public string GetCycleLabel(int weekNumber, bool isPartial)
        {
            return isPartial ? $"Week {weekNumber} (Partial)" : $"Week {weekNumber}";
        }

        public async Task InitializeNewCycleAsync(int clientUrlId, DateOnly newCycleStartDate, int newWeekNumber)
        {
            var cu = await _db.ClientUrls.FindAsync(clientUrlId);
            if (cu == null) return;
            cu.CycleStartDate = newCycleStartDate;
            cu.CycleWeekNumber = newWeekNumber;
            await _db.SaveChangesAsync();
        }
    }
}
