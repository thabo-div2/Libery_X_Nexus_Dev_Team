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
    /// <summary>
    /// Controller for managing invitations. Provides endpoints for advisors to create invitations and for validating invitation tokens.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InvitationController : ControllerBase
    {
        private readonly IInvitationService _invitationService;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="InvitationController"/> class with the specified invitation service.
        /// </summary>
        /// <param name="invitationService"></param>
        public InvitationController(IInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current advisor's ID from their login token so they can only access their own records.
        /// </summary>
        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Creates a new invitation for a client to join the platform. 
        /// Only accessible by users with the "Advisor" role. 
        /// The advisor's ID is extracted from their login token to ensure they can only create invitations for their own clients.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("create")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<InvitationResponse>> Create([FromBody] CreateInvitationRequest request)
        {
            var result = await _invitationService.CreateAsync(CurrentAdvisorId, request.Email);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Validates an invitation token to check if it is valid and not expired.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet("validate/{token}")]
        [AllowAnonymous]
        public async Task<ActionResult<InvitationValidationResponse>> Validate(string token)
        {
            var result = await _invitationService.ValidateAsync(token);
            return Ok(result); // Always 200 — Valid=false in the body carries the failure reason.
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
