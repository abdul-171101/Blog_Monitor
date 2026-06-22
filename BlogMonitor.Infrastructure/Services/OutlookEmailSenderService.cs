using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using BlogMonitor.Core.Interfaces;
using BlogMonitor.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BlogMonitor.Infrastructure.Services
{
    public class OutlookEmailSenderService : IOutlookEmailSenderService
    {
        private readonly IDuckDbReportService _duck;
        private readonly IConfiguration _config;
        private readonly ILogger<OutlookEmailSenderService> _logger;

        public OutlookEmailSenderService(IDuckDbReportService duck, IConfiguration config, ILogger<OutlookEmailSenderService> logger)
        {
            _duck = duck;
            _config = config;
            _logger = logger;
        }

        public async Task SendWeeklyReportAsync(string toEmail, string clientName, long weeklyReportId)
        {
            var attempts = 0;
            var maxAttempts = 3;
            var sent = false;
            var lastEx = (Exception?)null;

            var report = await _duck.GetWeeklyReportDetailAsync(weeklyReportId);
            if (report == null)
            {
                _logger.LogError("Weekly report {Id} not found", weeklyReportId);
                return;
            }

            var pdf = report.MdFilePath != null ? Path.ChangeExtension(report.MdFilePath, ".pdf") : null;
            var docx = report.MdFilePath != null ? Path.ChangeExtension(report.MdFilePath, ".docx") : null;

            var smtpHost = _config["Email:SmtpHost"];
            var smtpPort = int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;
            var senderEmail = _config["Email:SenderEmail"];
            var senderPassword = _config["Email:SenderPassword"];

            var cycleLabel = report.CycleLabel ?? $"Week {report.WeekNumber}";
            var subject = $"[Blog Monitor] Weekly Report — {clientName} — {cycleLabel} ({report.WeekStartDate} to {report.WeekEndDate})";

            var htmlBody = $@"<p>Hi {clientName},</p>
<p>Attached is your weekly report for {report.WeekStartDate} to {report.WeekEndDate}.</p>
<p>Summary: Total Days: {report.TotalDays}, Days Changed: {report.DaysChanged}, Days No Change: {report.DaysNoChange}, Total Pages Scraped: {report.TotalPagesScraped}</p>
<p>Regards,<br/>Blog Monitor</p>";

            while (attempts < maxAttempts && !sent)
            {
                attempts++;
                try
                {
                    var msg = new MimeMessage();
                    msg.From.Add(MailboxAddress.Parse(senderEmail));
                    msg.To.Add(MailboxAddress.Parse(toEmail));
                    msg.Subject = subject;

                    var builder = new BodyBuilder { HtmlBody = htmlBody };
                    if (pdf != null && File.Exists(pdf)) builder.Attachments.Add(Path.GetFileName(pdf), File.ReadAllBytes(pdf));
                    if (docx != null && File.Exists(docx)) builder.Attachments.Add(Path.GetFileName(docx), File.ReadAllBytes(docx));

                    msg.Body = builder.ToMessageBody();

                    using var client = new SmtpClient();
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(senderEmail, senderPassword);
                    await client.SendAsync(msg);
                    await client.DisconnectAsync(true);

                    await _duck.UpdateEmailStatusAsync(weeklyReportId, "SENT", DateTime.UtcNow);
                    sent = true;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    _logger.LogError(ex, "Failed to send weekly report attempt {Attempt} for report {Id}", attempts, weeklyReportId);
                    await Task.Delay(5000);
                }
            }

            if (!sent)
            {
                _logger.LogError(lastEx, "All attempts failed sending weekly report {Id}", weeklyReportId);
                await _duck.UpdateEmailStatusAsync(weeklyReportId, "FAILED", null);
            }
        }
    }
}
