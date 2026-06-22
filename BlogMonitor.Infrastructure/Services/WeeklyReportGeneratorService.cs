using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BlogMonitor.Core.Interfaces;
using BlogMonitor.Infrastructure.DuckDb;
using BlogMonitor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BlogMonitor.Infrastructure.Services
{
    public class WeeklyReportGeneratorService : IWeeklyReportGeneratorService
    {
        private record DailyEntry(DateOnly Date, string? Status, int PagesChanged, int PagesNoChange, int PagesFailed, string CompiledSummary);

        private readonly IDuckDbReportService _duck;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<WeeklyReportGeneratorService> _logger;
        private readonly DuckDb.DuckDbContext _duckContext;

        public WeeklyReportGeneratorService(IDuckDbReportService duck, ApplicationDbContext db, IConfiguration config, ILogger<WeeklyReportGeneratorService> logger, DuckDb.DuckDbContext duckContext)
        {
            _duck = duck;
            _db = db;
            _config = config;
            _logger = logger;
            _duckContext = duckContext;
        }

        public async Task GenerateAndStoreAsync(int clientUrlId, int clientId, int weekNumber, DateOnly cycleStartDate, DateOnly cycleEndDate)
        {
            try
            {
                // Query daily_summaries from DuckDB
                var conn = _duckContext.Connection;
                var entries = new List<DailyEntry>();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT summary_date, status, pages_changed, pages_no_change, pages_failed, compiled_summary FROM daily_summaries WHERE client_url_id = ? AND summary_date >= ? AND summary_date <= ? ORDER BY summary_date ASC;";
                    var p = cmd.CreateParameter(); p.ParameterName = "@client_url_id"; p.Value = clientUrlId; p.DbType = System.Data.DbType.Int32; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@start"; p.Value = cycleStartDate.ToString("yyyy-MM-dd"); p.DbType = System.Data.DbType.Date; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@end"; p.Value = cycleEndDate.ToString("yyyy-MM-dd"); p.DbType = System.Data.DbType.Date; cmd.Parameters.Add(p);
                    using var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        var date = DateOnly.Parse(rdr.GetString(0));
                        var status = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                        var changed = rdr.IsDBNull(2) ? 0 : rdr.GetInt32(2);
                        var nochange = rdr.IsDBNull(3) ? 0 : rdr.GetInt32(3);
                        var failed = rdr.IsDBNull(4) ? 0 : rdr.GetInt32(4);
                        var compiled = rdr.IsDBNull(5) ? string.Empty : rdr.GetString(5);
                        entries.Add(new DailyEntry(date, status, changed, nochange, failed, compiled));
                    }
                }

                var clientUrl = await _db.ClientUrls.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == clientUrlId);
                if (clientUrl == null)
                {
                    _logger.LogWarning("ClientUrl {Id} not found for weekly report", clientUrlId);
                    return;
                }

                var reportObj = new
                {
                    ClientName = clientUrl.Client?.FullName ?? string.Empty,
                    ClientEmail = clientUrl.Client?.Email ?? string.Empty,
                    MonitoredUrl = clientUrl.Url,
                    WeekNumber = weekNumber,
                    CycleStartDate = cycleStartDate,
                    CycleEndDate = cycleEndDate,
                    GeneratedAt = DateTime.UtcNow,
                    Days = entries.Select(e => new
                    {
                        Date = e.Date,
                        Status = e.Status,
                        PagesChanged = e.PagesChanged,
                        PagesNoChange = e.PagesNoChange,
                        PagesFailed = e.PagesFailed,
                        CompiledSummary = e.CompiledSummary
                    }).ToList()
                };

                var reportJson = JsonSerializer.Serialize(reportObj, new JsonSerializerOptions { WriteIndented = true });

                var basePath = _config.GetValue<string>("Storage:BasePath") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BlogMonitorStorage");
                var dir = Path.Combine(basePath, "reports", clientId.ToString(), clientUrlId.ToString());
                Directory.CreateDirectory(dir);
                var fileBase = Path.Combine(dir, $"{DateTime.UtcNow:yyyy}-W{weekNumber:D2}");
                var mdPath = fileBase + ".md";
                var pdfPath = fileBase + ".pdf";
                var docxPath = fileBase + ".docx";

                // Markdown
                using (var sw = File.CreateText(mdPath))
                {
                    sw.WriteLine($"# Weekly Report — {clientUrl.Client?.FullName}");
                    sw.WriteLine();
                    sw.WriteLine($"URL: {clientUrl.Url}");
                    sw.WriteLine($"Week: {cycleStartDate} - {cycleEndDate}");
                    sw.WriteLine();
                    foreach (var d in entries)
                    {
                        sw.WriteLine($"## {d.Date}");
                        sw.WriteLine($"Status: {d.Status}");
                        sw.WriteLine();
                        sw.WriteLine(d.CompiledSummary ?? string.Empty);
                        sw.WriteLine();
                    }
                }

                // PDF via QuestPDF (simple layout)
                var pdfDoc = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(30);
                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Weekly Report — {clientUrl.Client?.FullName}").FontSize(20).Bold();
                                col.Item().Text($"{clientUrl.Url}");
                                col.Item().Text($"Week: {cycleStartDate} - {cycleEndDate}");
                            });
                        });

                        page.Content().Column(col =>
                        {
                            foreach (var d in entries)
                            {
                                col.Item().Text($"{d.Date} — {d.Status}").Bold();
                                col.Item().Text(d.CompiledSummary ?? string.Empty).FontSize(12);
                                col.Item().PaddingBottom(10);
                            }
                        });
                    });
                });
                pdfDoc.GeneratePdf(pdfPath);

                // DOCX via OpenXML
                using (var word = WordprocessingDocument.Create(docxPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    var main = word.AddMainDocumentPart();
                    main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                    var body = main.Document.AppendChild(new Body());
                    body.AppendChild(new Paragraph(new Run(new Text($"Weekly Report — {clientUrl.Client?.FullName}"))));
                    body.AppendChild(new Paragraph(new Run(new Text($"URL: {clientUrl.Url}"))));
                    body.AppendChild(new Paragraph(new Run(new Text($"Week: {cycleStartDate} - {cycleEndDate}"))));
                    foreach (var d in entries)
                    {
                        body.AppendChild(new Paragraph(new Run(new Text(d.Date.ToString()))));
                        body.AppendChild(new Paragraph(new Run(new Text($"Status: {d.Status}"))));
                        body.AppendChild(new Paragraph(new Run(new Text(d.CompiledSummary ?? string.Empty))));
                    }
                }

                // compute stats
                int totalDays = entries.Count;
                int daysChanged = entries.Count(e => e.Status == "CHANGED");
                int daysNoChange = entries.Count(e => e.Status == "NO_CHANGE");
                int totalPages = entries.Sum(e => e.PagesChanged + e.PagesNoChange + e.PagesFailed);

                await _duck.InsertWeeklyReportAsync(clientUrlId, clientId, cycleStartDate, cycleEndDate, weekNumber, _weeklyLabel(weekNumber, cycleStartDate), reportJson, mdPath, pdfPath, docxPath, totalDays, daysChanged, daysNoChange, totalPages);

                _logger.LogInformation("Weekly report generated for ClientUrl {Id}", clientUrlId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate weekly report for ClientUrl {Id}", clientUrlId);
                throw;
            }
        }

        private string _weeklyLabel(int weekNumber, DateOnly cycleStartDate)
        {
            bool isPartial = cycleStartDate.DayOfWeek != DayOfWeek.Monday;
            return isPartial ? $"Week {weekNumber} (Partial)" : $"Week {weekNumber}";
        }
    }
}
