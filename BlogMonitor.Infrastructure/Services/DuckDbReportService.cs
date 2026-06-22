using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using BlogMonitor.Infrastructure.DuckDb;
using BlogMonitor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlogMonitor.Infrastructure.Services
{
    public class DuckDbReportService : IDuckDbReportService
    {
        private static readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly DuckDbContext _duck;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DuckDbReportService> _logger;

        public DuckDbReportService(DuckDbContext duck, ApplicationDbContext db, ILogger<DuckDbReportService> logger)
        {
            _duck = duck ?? throw new ArgumentNullException(nameof(duck));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger;
        }

        public async Task InsertPageSummaryAsync(int clientUrlId, string pageUrl, string? pageTitle, DateOnly summaryDate, int? weekNumber, int? cycleDayNumber, string mdFilePath, string summaryText, int wordCount, string summaryStatus)
        {
            await _writeLock.WaitAsync();
            try
            {
                var conn = _duck.Connection;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO page_summaries (id, client_url_id, page_url, page_title, summary_date, week_number, cycle_day_number, md_file_path, summary_text, word_count, summary_status, created_at)
VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP);";

                var id = DateTime.UtcNow.Ticks;
                var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = id; p.DbType = DbType.Int64; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@client_url_id"; p.Value = clientUrlId; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@page_url"; p.Value = pageUrl; p.DbType = DbType.String; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@page_title"; p.Value = (object?)pageTitle ?? DBNull.Value; p.DbType = DbType.String; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@summary_date"; p.Value = summaryDate.ToString("yyyy-MM-dd"); p.DbType = DbType.Date; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@week_number"; p.Value = (object?)weekNumber ?? DBNull.Value; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@cycle_day_number"; p.Value = (object?)cycleDayNumber ?? DBNull.Value; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@md_file_path"; p.Value = (object?)mdFilePath ?? DBNull.Value; p.DbType = DbType.String; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@summary_text"; p.Value = (object?)summaryText ?? DBNull.Value; p.DbType = DbType.String; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@word_count"; p.Value = wordCount; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@summary_status"; p.Value = (object?)summaryStatus ?? DBNull.Value; p.DbType = DbType.String; cmd.Parameters.Add(p);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert page summary for {ClientUrlId} {Url}", clientUrlId, pageUrl);
                throw;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task InsertDailySummaryAsync(DailySummaryRecord record)
        {
            await _writeLock.WaitAsync();
            try
            {
                var conn = _duck.Connection;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO daily_summaries (id, client_url_id, summary_date, day_in_cycle, status, pages_changed, pages_no_change, pages_failed, compiled_summary, created_at)
VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP);";

                var id = DateTime.UtcNow.Ticks;
                var compiled = record.PageSummaries == null || record.PageSummaries.Count == 0 ? string.Empty : string.Join("\n\n---\n\n", record.PageSummaries);

                var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = id; p.DbType = DbType.Int64; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@client_url_id"; p.Value = record.ClientUrlId; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@summary_date"; p.Value = record.Date.ToString("yyyy-MM-dd"); p.DbType = DbType.Date; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@day_in_cycle"; p.Value = DBNull.Value; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@status"; p.Value = record.Status; p.DbType = DbType.String; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@pages_changed"; p.Value = record.PagesChanged; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@pages_no_change"; p.Value = record.PagesNoChange; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@pages_failed"; p.Value = record.PagesFailed; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@compiled_summary"; p.Value = (object?)compiled ?? DBNull.Value; p.DbType = DbType.String; cmd.Parameters.Add(p);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert daily summary for {ClientUrlId}", record.ClientUrlId);
                throw;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task InsertWeeklyReportAsync(int clientUrlId, int clientId, DateOnly weekStartDate, DateOnly weekEndDate, int weekNumber, string cycleLabel, string reportJson, string mdFilePath, string pdfFilePath, string docxFilePath, int totalDays, int daysChanged, int daysNoChange, int totalPagesScraped)
        {
            await _writeLock.WaitAsync();
            try
            {
                var conn = _duck.Connection;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO weekly_reports (id, client_url_id, client_id, week_start_date, week_end_date, week_number, cycle_label, report_json, md_file_path, pdf_file_path, docx_file_path, total_days, days_changed, days_no_change, total_pages_scraped, email_status, created_at)
VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP);";

                var id = DateTime.UtcNow.Ticks;
                var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = id; p.DbType = DbType.Int64; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@client_url_id"; p.Value = clientUrlId; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@client_id"; p.Value = clientId; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@week_start_date"; p.Value = weekStartDate.ToString("yyyy-MM-dd"); p.DbType = DbType.Date; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@week_end_date"; p.Value = weekEndDate.ToString("yyyy-MM-dd"); p.DbType = DbType.Date; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@week_number"; p.Value = weekNumber; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@cycle_label"; p.Value = (object?)cycleLabel ?? DBNull.Value; p.DbType = DbType.String; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@report_json"; p.Value = (object?)reportJson ?? DBNull.Value; p.DbType = DbType.String; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@md_file_path"; p.Value = (object?)mdFilePath ?? DBNull.Value; p.DbType = DbType.String; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@pdf_file_path"; p.Value = (object?)pdfFilePath ?? DBNull.Value; p.DbType = DbType.String; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@docx_file_path"; p.Value = (object?)docxFilePath ?? DBNull.Value; p.DbType = DbType.String; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@total_days"; p.Value = totalDays; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@days_changed"; p.Value = daysChanged; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@days_no_change"; p.Value = daysNoChange; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@total_pages_scraped"; p.Value = totalPagesScraped; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@email_status"; p.Value = "PENDING"; p.DbType = DbType.String; cmd.Parameters.Add(p);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert weekly report for {ClientUrlId}", clientUrlId);
                throw;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task UpdateEmailStatusAsync(long reportId, string emailStatus, DateTime? emailSentAt)
        {
            await _writeLock.WaitAsync();
            try
            {
                var conn = _duck.Connection;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"UPDATE weekly_reports SET email_status = ?, email_sent_at = ? WHERE id = ?;";
                var p = cmd.CreateParameter(); p.ParameterName = "@email_status"; p.Value = emailStatus; p.DbType = DbType.String; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@email_sent_at"; p.Value = (object?)emailSentAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value; p.DbType = DbType.DateTime; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = reportId; p.DbType = DbType.Int64; cmd.Parameters.Add(p);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update email status for report {ReportId}", reportId);
                throw;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task<List<WeeklyReportListItem>> GetWeeklyReportsAsync(int clientUrlId)
        {
            var list = new List<WeeklyReportListItem>();
            try
            {
                var conn = _duck.Connection;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT id, client_url_id, week_start_date, week_end_date, week_number, cycle_label, total_days, days_changed, days_no_change, total_pages_scraped, email_status, created_at FROM weekly_reports WHERE client_url_id = ? ORDER BY week_start_date DESC;";
                var p = cmd.CreateParameter(); p.ParameterName = "@client_url_id"; p.Value = clientUrlId; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var id = (long)rdr.GetInt64(0);
                    var clientUrl = rdr.GetInt32(1);
                    DateOnly? ws = rdr.IsDBNull(2) ? null : DateOnly.Parse(rdr.GetString(2));
                    DateOnly? we = rdr.IsDBNull(3) ? null : DateOnly.Parse(rdr.GetString(3));
                    int? weekNum = rdr.IsDBNull(4) ? null : (int?)rdr.GetInt32(4);
                    string? cycleLabel = rdr.IsDBNull(5) ? null : rdr.GetString(5);
                    int totalDays = rdr.IsDBNull(6) ? 0 : rdr.GetInt32(6);
                    int daysChanged = rdr.IsDBNull(7) ? 0 : rdr.GetInt32(7);
                    int daysNoChange = rdr.IsDBNull(8) ? 0 : rdr.GetInt32(8);
                    int totalPages = rdr.IsDBNull(9) ? 0 : rdr.GetInt32(9);
                    string emailStatus = rdr.IsDBNull(10) ? "" : rdr.GetString(10);
                    DateTime createdAt = rdr.IsDBNull(11) ? DateTime.UtcNow : DateTime.Parse(rdr.GetString(11));

                    // fetch Url from SQL Server
                    var cu = await _db.ClientUrls.FindAsync(clientUrl);
                    var url = cu?.Url ?? string.Empty;

                    list.Add(new WeeklyReportListItem(id, clientUrl, url, ws, we, weekNum, cycleLabel, totalDays, daysChanged, daysNoChange, totalPages, emailStatus, createdAt));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query weekly reports for {ClientUrlId}", clientUrlId);
                throw;
            }

            return list;
        }

        public async Task<WeeklyReportDetailResponse?> GetWeeklyReportDetailAsync(long reportId)
        {
            try
            {
                var conn = _duck.Connection;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT id, client_url_id, week_start_date, week_end_date, week_number, cycle_label, report_json, md_file_path, total_days, days_changed, days_no_change, total_pages_scraped, email_status, email_sent_at FROM weekly_reports WHERE id = ?;";
                var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = reportId; p.DbType = DbType.Int64; cmd.Parameters.Add(p);
                using var rdr = cmd.ExecuteReader();
                if (!rdr.Read()) return null;

                var id = (long)rdr.GetInt64(0);
                var clientUrl = rdr.GetInt32(1);
                DateOnly? ws = rdr.IsDBNull(2) ? null : DateOnly.Parse(rdr.GetString(2));
                DateOnly? we = rdr.IsDBNull(3) ? null : DateOnly.Parse(rdr.GetString(3));
                int? weekNum = rdr.IsDBNull(4) ? null : (int?)rdr.GetInt32(4);
                string? cycleLabel = rdr.IsDBNull(5) ? null : rdr.GetString(5);
                string? reportJson = rdr.IsDBNull(6) ? null : rdr.GetString(6);
                string? md = rdr.IsDBNull(7) ? null : rdr.GetString(7);
                int totalDays = rdr.IsDBNull(8) ? 0 : rdr.GetInt32(8);
                int daysChanged = rdr.IsDBNull(9) ? 0 : rdr.GetInt32(9);
                int daysNoChange = rdr.IsDBNull(10) ? 0 : rdr.GetInt32(10);
                int totalPages = rdr.IsDBNull(11) ? 0 : rdr.GetInt32(11);
                string emailStatus = rdr.IsDBNull(12) ? string.Empty : rdr.GetString(12);
                DateTime? emailSentAt = rdr.IsDBNull(13) ? null : DateTime.Parse(rdr.GetString(13));

                var cu = await _db.ClientUrls.FindAsync(clientUrl);
                var url = cu?.Url ?? string.Empty;

                return new WeeklyReportDetailResponse(id, clientUrl, url, ws, we, weekNum, cycleLabel, reportJson, md, totalDays, daysChanged, daysNoChange, totalPages, emailStatus, emailSentAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get weekly report detail {ReportId}", reportId);
                throw;
            }
        }

        public async Task<DailySummaryResponse?> GetDailySummaryAsync(int clientUrlId, DateOnly date)
        {
            try
            {
                var conn = _duck.Connection;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT id, client_url_id, summary_date, day_in_cycle, status, pages_changed, pages_no_change, pages_failed, compiled_summary, created_at FROM daily_summaries WHERE client_url_id = ? AND summary_date = ? LIMIT 1;";
                var p = cmd.CreateParameter(); p.ParameterName = "@client_url_id"; p.Value = clientUrlId; p.DbType = DbType.Int32; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@date"; p.Value = date.ToString("yyyy-MM-dd"); p.DbType = DbType.Date; cmd.Parameters.Add(p);
                using var rdr = cmd.ExecuteReader();
                if (!rdr.Read()) return null;

                var id = (long)rdr.GetInt64(0);
                var cu = rdr.GetInt32(1);
                DateOnly sd = DateOnly.Parse(rdr.GetString(2));
                int? dayInCycle = rdr.IsDBNull(3) ? null : (int?)rdr.GetInt32(3);
                string? status = rdr.IsDBNull(4) ? null : rdr.GetString(4);
                int pagesChanged = rdr.IsDBNull(5) ? 0 : rdr.GetInt32(5);
                int pagesNoChange = rdr.IsDBNull(6) ? 0 : rdr.GetInt32(6);
                int pagesFailed = rdr.IsDBNull(7) ? 0 : rdr.GetInt32(7);
                string? compiled = rdr.IsDBNull(8) ? null : rdr.GetString(8);
                DateTime createdAt = rdr.IsDBNull(9) ? DateTime.UtcNow : DateTime.Parse(rdr.GetString(9));

                return new DailySummaryResponse(id, cu, sd, dayInCycle, status, pagesChanged, pagesNoChange, pagesFailed, compiled, createdAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get daily summary for {ClientUrlId} {Date}", clientUrlId, date);
                throw;
            }
        }
    }
}
