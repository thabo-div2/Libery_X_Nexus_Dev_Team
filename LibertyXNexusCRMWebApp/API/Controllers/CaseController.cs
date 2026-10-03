using API.DTOs.Cases;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;

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

        [HttpPut("{id:int}/steps/{step}")]
        public async Task<ActionResult<CaseStatusDto>> MarkStepComplete(int id, CaseStep step)
        {
            try
            {
                return Ok(await _caseService.MarkStepCompleteAsync(id,step));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new {message = ex.Message});
            }
        }
    }
}
