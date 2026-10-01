using API.DTOs.Invitations;
using API.Identity;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using Shared.Models.Enums;
using System.Security.Claims;
using System.Security.Cryptography;

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

        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        [HttpPost("create")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<InvitationResponse>> Create([FromBody] CreateInvitationRequest request)
        {
            var result = await _invitationService.CreateAsync(CurrentAdvisorId, request.Email);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("validate/{token}")]
        [AllowAnonymous]
        public async Task<ActionResult<InvitationValidationResponse>> Validate(string token)
        {
            var result = await _invitationService.ValidateAsync(token);
            return Ok(result); // Always 200 — Valid=false in the body carries the failure reason.
        }
    }
}
