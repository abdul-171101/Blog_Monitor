using System.Threading.Tasks;

namespace BlogMonitor.Core.Interfaces
{
    public interface IOutlookEmailSenderService
    {
        Task SendWeeklyReportAsync(string toEmail, string clientName, long weeklyReportId);
    }
}
