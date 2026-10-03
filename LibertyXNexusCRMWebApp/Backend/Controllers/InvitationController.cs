using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InvitationController : ControllerBase
    {
        private readonly InvitationService _invitationService;

        public InvitationController(InvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(CreateInvitationRequest request)
        {
            var result = await _invitationService.CreateAsync(request);
            return Ok(result);
        }

        [HttpGet("validate/{token}")]
        public async Task<IActionResult> Validate(string token)
        {
            var result = await _invitationService.ValidateAsync(token);
            return Ok(result);
        }
    }
}
