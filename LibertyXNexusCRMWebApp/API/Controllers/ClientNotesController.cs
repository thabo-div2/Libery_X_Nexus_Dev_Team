using API.DTOs.Notes;
using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// The advisor's private notes about their clients. Advisors only; clients have no access at all.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Advisor)]
    public class ClientNotesController : ControllerBase
    {
        private readonly IClientNoteService _clientNoteService;

        public ClientNotesController(IClientNoteService clientNoteService)
        {
            _clientNoteService = clientNoteService;
        }

        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the notes about one of the advisor's clients.
        /// </summary>
        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<ClientNoteDto>>> GetForClient(int clientId)
        {
            try
            {
                return Ok(await _clientNoteService.GetForClientAsync(clientId, CurrentAdvisorId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Adds a note about one of the advisor's clients.
        /// </summary>
        [HttpPost("client/{clientId:int}")]
        public async Task<ActionResult<ClientNoteDto>> Create(int clientId, [FromBody] CreateClientNoteRequest request)
        {
            try
            {
                return Ok(await _clientNoteService.CreateAsync(clientId, CurrentAdvisorId, request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes a note.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _clientNoteService.DeleteAsync(id, CurrentAdvisorId);
            return deleted ? NoContent() : NotFound(new { message = "That note could not be found." });
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
