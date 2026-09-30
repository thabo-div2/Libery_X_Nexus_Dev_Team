using API.Identity;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using Shared.Models.Enums;
using System.Security.Claims;
using System.Security.Cryptography;
using static API.Services.Implementations.InvitationService;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvitationController : ControllerBase
    {
        private readonly IInvitationService _invitationService;

        public InvitationController(IInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        [HttpPost("create")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<InvitationResult>> Create([FromBody] CreateInvitationRequest request)
        {
            if (!int.TryParse(User.FindFirstValue("advisorId"), out var advisorId))
                return Unauthorized(new { message = "The authenticated advisor is missing an advisorId claim." });

            var invitation = await _invitationService.CreateInvitation(request, advisorId);

            return Ok(invitation);
        }

        [HttpGet("validate/{token}")]
        [AllowAnonymous]
        public async Task<ActionResult<InvitationDetails>> Validate(string token)
        {
            var invitation = await _invitationService.Validate(token);

            return Ok(invitation);
        }
    }
}
