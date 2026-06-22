using System;
using System.Linq;
using System.Threading.Tasks;
using BlogMonitor.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogMonitor.Api.Controllers
{
    [ApiController]
    [Route("api/logs")]
    [Authorize(Roles = "Admin")]
    public class LogController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public LogController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? clientUrlId, [FromQuery] string? level, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var q = _db.MonitoringLogs.AsQueryable();
            if (clientUrlId.HasValue) q = q.Where(l => l.ClientUrlId == clientUrlId.Value);
            if (!string.IsNullOrWhiteSpace(level)) q = q.Where(l => l.Level == level);
            if (fromDate.HasValue) q = q.Where(l => l.Timestamp >= fromDate.Value);
            if (toDate.HasValue) q = q.Where(l => l.Timestamp <= toDate.Value);

            var items = await q.OrderByDescending(l => l.Timestamp).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var clientIds = items.Select(i => i.ClientUrlId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var urls = await _db.ClientUrls.Where(u => clientIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Url);

            var resp = items.Select(i => new Core.DTOs.MonitoringLogResponse(i.Id, i.ClientUrlId, i.ClientUrlId.HasValue && urls.ContainsKey(i.ClientUrlId.Value) ? urls[i.ClientUrlId.Value] : null, i.EventType, i.Message ?? string.Empty, i.Level ?? string.Empty, i.Timestamp)).ToList();

            return Ok(resp);
        }
    }
}
