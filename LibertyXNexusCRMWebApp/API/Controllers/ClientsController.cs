using API.DTOs.Clients;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;
        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet]
        public async Task <ActionResult<IEnumerable<ClientListItemDto>>> Search([FromQuery] string? searchTerm, [FromQuery] ClientStatus? status, [FromQuery] int? advisorId)
        {
            var results = await _clientService.SearchAsync(searchTerm, status, advisorId);
            return Ok(results);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ClientDetailDto>> GetById(int id)
        {
            var client = await _clientService.GetByIdAsync(id);
            if (client is null)
            {
                return NotFound(new { message = $"Client {id} was not found" });
            }
            return Ok(client);
        }

        [HttpPost]
        public async Task<ActionResult<ClientDetailDto>> Create([FromBody] CreateClientRequest request)
        {
            try
            {
                var created = await _clientService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.ClientId }, created);

            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ClientDetailDto>> Update(int id, [FromBody] UpdateClientRequest request)
        {
            var updated = await _clientService.UpdateAsync(id, request);
            if (updated is null)
            {
                return NotFound(new { message = $"Client {id} was not found" });
            }
            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _clientService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Client {id} was not found" });
            }
            return NoContent();
        }
    }
}
