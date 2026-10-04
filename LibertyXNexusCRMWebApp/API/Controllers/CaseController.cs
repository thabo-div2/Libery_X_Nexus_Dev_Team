using API.DTOs.Cases;
using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Provides access to client case information.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CasesController : ControllerBase
    {
        private readonly ICaseService _caseService;
        private readonly IClientService _clientService;

        public CasesController(ICaseService caseService,IClientService clientService)
        {
            _caseService = caseService;
            _clientService = clientService;
        }

        private bool IsAdvisor =>
            User.IsInRole(AppRoles.Advisor);

        private int CurrentAdvisorId =>
            int.TryParse(User.FindFirstValue("advisorId"), out var id)
                ? id
                : throw new InvalidOperationException(
                    "Token has no advisorId claim.");

        private int CurrentClientId =>
            int.TryParse(User.FindFirstValue("clientId"), out var id)
                ? id
                : throw new InvalidOperationException(
                    "Token has no clientId claim.");

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Determines whether the authenticated user is allowed to access
        /// records belonging to the specified client.
        /// </summary>
        private async Task<bool> CanAccessClientAsync(int clientId)
        {
            // Clients may only access their own records.
            if (!IsAdvisor)
            {
                return CurrentClientId == clientId;
            }

            // Advisors may only access clients assigned to them.
            var client = await _clientService.GetByIdAsync(clientId);

            return client is not null &&
                   client.AdvisorId == CurrentAdvisorId;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all cases belonging to a specific client.
        /// </summary>
        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<CaseStatusDto>>> GetForClient(
            int clientId)
        {
            if (!await CanAccessClientAsync(clientId))
            {
                // Do not reveal whether another client's records exist.
                return NotFound(new
                {
                    message = "Client cases were not found."
                });
            }

            var cases = await _caseService.GetForClientAsync(clientId);

            return Ok(cases);
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
