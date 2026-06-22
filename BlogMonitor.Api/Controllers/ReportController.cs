using System;
using System.IO;
using System.Threading.Tasks;
using BlogMonitor.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogMonitor.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Admin,Viewer")]
    public class ReportController : ControllerBase
    {
        private readonly IReportQueryService _reports;

        public ReportController(IReportQueryService reports)
        {
            _reports = reports;
        }

        [HttpGet("weekly")]
        public async Task<IActionResult> GetWeekly([FromQuery] int clientUrlId)
        {
            var res = await _reports.GetWeeklyReportsAsync(clientUrlId);
            return Ok(res);
        }

        [HttpGet("weekly/{id:long}")]
        public async Task<IActionResult> GetWeeklyDetail([FromRoute] long id)
        {
            var r = await _reports.GetWeeklyReportDetailAsync(id);
            if (r == null) return NotFound();
            return Ok(r);
        }

        [HttpGet("weekly/{id:long}/download")]
        public async Task<IActionResult> Download([FromRoute] long id, [FromQuery] string format)
        {
            var r = await _reports.GetWeeklyReportDetailAsync(id);
            if (r == null) return NotFound();
            string? path = null;
            string contentType = "application/octet-stream";
            if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                path = r.MdFilePath != null ? Path.ChangeExtension(r.MdFilePath, ".pdf") : null;
                contentType = "application/pdf";
            }
            else if (string.Equals(format, "docx", StringComparison.OrdinalIgnoreCase))
            {
                path = r.MdFilePath != null ? Path.ChangeExtension(r.MdFilePath, ".docx") : null;
                contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            }
            else
            {
                return BadRequest("format must be 'pdf' or 'docx'");
            }

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return NotFound();
            var stream = System.IO.File.OpenRead(path);
            var name = Path.GetFileName(path);
            return File(stream, contentType, name);
        }

        [HttpPost("weekly/{id:long}/resend")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Resend([FromRoute] long id)
        {
            await _reports.ResendEmailAsync(id);
            return Accepted();
        }

        [HttpGet("daily")]
        public async Task<IActionResult> GetDaily([FromQuery] int clientUrlId, [FromQuery] DateOnly date)
        {
            var r = await _reports.GetDailySummaryAsync(clientUrlId, date);
            if (r == null) return NotFound();
            return Ok(r);
        }
    }
}
