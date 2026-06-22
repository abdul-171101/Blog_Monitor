using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace BlogMonitor.Api.Controllers
{
    [ApiController]
    [Route("api/clients")]
    [Authorize(Roles = "Admin")]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _service;

        public ClientController(IClientService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<ClientResponse>>> GetAll()
        {
            var r = await _service.GetAllClientsAsync();
            return Ok(r);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateClientRequest req)
        {
            try
            {
                var created = await _service.CreateClientAsync(req);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (System.InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _service.GetClientByIdAsync(id);
            if (c == null) return NotFound();
            return Ok(c);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateClientRequest req)
        {
            var updated = await _service.UpdateClientAsync(id, req);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.SoftDeleteClientAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
