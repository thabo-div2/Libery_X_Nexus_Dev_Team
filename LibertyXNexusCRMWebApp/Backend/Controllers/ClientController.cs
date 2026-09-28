using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClientController : ControllerBase
    {
        private readonly ClientService _clientService;

        public ClientController(ClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? search, [FromQuery] int? advisorId)
        {
            var results = await _clientService.SearchAsync(search, advisorId);
            return Ok(results);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _clientService.GetByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }
    }
}
