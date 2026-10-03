using API.DTOs.Cases;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CasesController : ControllerBase
    {
        private readonly ICaseService _caseService;

        public CasesController(ICaseService caseService)
        {
            _caseService = caseService;
        }

        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<CaseStatusDto>>> GetForClient(int clientId)
        {
            return Ok(await _caseService.GetForClientAsync(clientId));
        }
    }
}
