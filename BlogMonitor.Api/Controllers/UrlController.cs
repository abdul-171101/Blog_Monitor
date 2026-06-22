using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace BlogMonitor.Api.Controllers
{
    [ApiController]
    [Route("api/urls")]
    public class UrlController : ControllerBase
    {
        private readonly IClientUrlService _service;

        public UrlController(IClientUrlService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Viewer")]
        public async Task<ActionResult<List<UrlSummaryResponse>>> GetAll()
        {
            var r = await _service.GetAllUrlsAsync();
            return Ok(r);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] RegisterUrlRequest req)
        {
            try
            {
                var created = await _service.RegisterUrlAsync(req);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (System.InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Viewer")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _service.GetUrlByIdAsync(id);
            if (c == null) return NotFound();
            return Ok(c);
        }

        [HttpPut("{id}/stop")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Stop(int id)
        {
            var ok = await _service.StopMonitoringAsync(id);
            if (!ok) return NotFound();
            return Ok();
        }

        [HttpPost("{id}/run")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Run(int id)
        {
            var ok = await _service.TriggerManualRunAsync(id);
            if (!ok) return NotFound();
            return Accepted();
        }
    }
}
