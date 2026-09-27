using API.DTOs.Clients;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService clientService_;
        public ClientsController(IClientService clientService)
        {
            clientService_ = clientService;
        }

        [HttpGet]
        public async Task <ActionResult<IEnumerable<ClientListItemDto>>> Search([FromQuery] string? searchTerm, [FromQuery] ClientStatus? status, [FromQuery] int? advisorId)
        {
            var results = await clientService_.SearchAsync(searchTerm, status, advisorId);
            return Ok(results);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ClientDetailDto>> GetById(int id)
        {
            var client = await clientService_.GetByIdAsync(id);
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
                var created = await clientService_.CreateAsync(request);
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
            var updated = await clientService_.UpdateAsync(id, request);
            if (updated is null)
            {
                return NotFound(new { message = $"Client {id} was not found" });
            }
            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await clientService_.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Client {id} was not found" });
            }
            return NoContent();
        }
    }
}
