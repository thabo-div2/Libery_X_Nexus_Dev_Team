using API.DTOs.DocumentRequests;
using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Documents the advisor has asked a client for. The advisor creates and removes requests
    /// for their own clients; a client can only see their own.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentRequestsController : ControllerBase
    {
        private readonly IDocumentRequestService _documentRequestService;
        private readonly IClientService _clientService;

        public DocumentRequestsController(IDocumentRequestService documentRequestService, IClientService clientService)
        {
            _documentRequestService = documentRequestService;
            _clientService = clientService;
        }

        private bool IsAdvisor => User.IsInRole(AppRoles.Advisor);

        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        private int CurrentClientId => int.TryParse(User.FindFirstValue("clientId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no clientId claim.");

        /// <summary>
        /// Advisors can reach their own clients. Clients can only reach themselves.
        /// </summary>
        private async Task<bool> CanAccessClientAsync(int clientId)
        {
            if (!IsAdvisor)
            {
                return CurrentClientId == clientId;
            }

            var client = await _clientService.GetByIdAsync(clientId);
            return client is not null && client.AdvisorId == CurrentAdvisorId;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the documents a client has been asked for.
        /// </summary>
        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<DocumentRequestDto>>> GetForClient(int clientId)
        {
            if (!await CanAccessClientAsync(clientId))
            {
                // Do not reveal whether another client's records exist.
                return NotFound(new { message = "Document requests were not found." });
            }

            return Ok(await _documentRequestService.GetForClientAsync(clientId));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Asks a client for a document.
        /// </summary>
        [HttpPost("client/{clientId:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<DocumentRequestDto>> Create(int clientId, [FromBody] CreateDocumentRequestRequest request)
        {
            if (!await CanAccessClientAsync(clientId))
            {
                return NotFound(new { message = $"Client {clientId} was not found." });
            }

            try
            {
                return Ok(await _documentRequestService.CreateAsync(clientId, request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Asks several clients for the same general document at once (e.g. an annual tax certificate).
        /// Each client is handled on its own: one that already has an outstanding request, or isn't
        /// the advisor's client, is skipped with a reason, and the rest still go out.
        /// </summary>
        [HttpPost("bulk")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<Shared.DTOs.Insights.BulkDocumentRequestResult>> CreateBulk([FromBody] Shared.DTOs.Insights.BulkDocumentRequestInput input)
        {
            var mine = (await _clientService.SearchAsync(null, null, CurrentAdvisorId)).ToDictionary(c => c.ClientId);
            var skipped = new List<Shared.DTOs.Insights.BulkDocumentRequestSkip>();
            var created = 0;

            foreach (var clientId in input.ClientIds.Distinct())
            {
                if (!mine.TryGetValue(clientId, out var client))
                {
                    skipped.Add(new(clientId, $"Client {clientId}", "Not one of your clients."));
                    continue;
                }

                try
                {
                    await _documentRequestService.CreateAsync(clientId, new CreateDocumentRequestRequest
                    {
                        DocumentType = input.DocumentType,
                        Note = input.Note
                    });
                    created++;
                }
                catch (InvalidOperationException)
                {
                    skipped.Add(new(clientId, client.FullName, "Already has an outstanding request for this document."));
                }
                catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
                {
                    skipped.Add(new(clientId, client.FullName, ex.Message));
                }
            }

            return Ok(new Shared.DTOs.Insights.BulkDocumentRequestResult(created, skipped));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Removes a request the advisor made.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<IActionResult> Cancel(int id)
        {
            var removed = await _documentRequestService.CancelAsync(id, CurrentAdvisorId);
            return removed ? NoContent() : NotFound(new { message = "That request could not be found." });
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
