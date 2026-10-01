using API.DTOs.Documents;
using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService documentService_;
        private readonly IClientService _clientService;

        public DocumentsController(IDocumentService documentService, IClientService clientService)
        {
            documentService_ = documentService;
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
        /// True if the caller is allowed to see documents belonging to
        /// clientId - either they ARE that client, or they're the adviser
        /// who owns that client. Single DB lookup on the advisor path since
        /// AdvisorId isn't on the document itself.
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

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DocumentDto>> GetById(int id)
        {
            var document = await documentService_.GetByIdAsync(id);
            if (document is null || !await CanAccessClientAsync(document.ClientId))
            {
                // Same message either way - don't reveal whether the id
                // exists to a caller who isn't allowed to see it.
                return NotFound(new { message = $"Document {id} was not found" });
            }
            return Ok(document);
        }

        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetForClient(int clientId)
        {
            if (!await CanAccessClientAsync(clientId)) return Forbid();
            return Ok(await documentService_.GetForClientAsync(clientId));
        }

        [HttpGet("client/{clientId:int}/visible")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetVisibleToClient(int clientId)
        {
            if (!await CanAccessClientAsync(clientId)) return Forbid();
            return Ok(await documentService_.GetVisibleToClientAsync(clientId));
        }

        /// <summary>
        /// Advisor-only: this returns every document attached to a policy
        /// with no client scoping in the URL, so there's no cheap way to
        /// check "does this client own this policy" without an extra
        /// lookup. Restricting to the adviser (who legitimately sees all of
        /// their own clients' documents anyway) closes the hole without
        /// that extra complexity.
        /// </summary>
        [HttpGet("policy/{policyId:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetForPolicy(int policyId)
        {
            return Ok(await documentService_.GetForPolicyAsync(policyId));
        }

        /// <summary>
        /// Advisor-only - same reasoning as GetForPolicy: this returns
        /// documents across ALL clients with no per-client scoping at all.
        /// </summary>
        [HttpGet("type/{documentType}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetByType(DocumentType documentType)
        {
            return Ok(await documentService_.GetByTypeAsync(documentType));
        }

        /// <summary>
        /// Advisor-only, per the original requirements ("the financial
        /// planner uploads documents to specific clients"). Verifies the
        /// target client actually belongs to the uploading adviser.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Advisor)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(30_000_000)]
        public async Task<ActionResult<DocumentDto>> Upload([FromForm] UploadDocumentRequest request)
        {
            var client = await _clientService.GetByIdAsync(request.ClientId);
            if (client is null || client.AdvisorId != CurrentAdvisorId)
            {
                return Forbid();
            }

            // UploadedBy was previously trusted straight from the request
            // body (flagged with a TODO in the DTO) - now that auth exists,
            // it's set from the token instead so it can't be spoofed.
            request.UploadedBy = User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? $"advisor-{CurrentAdvisorId}";

            try
            {
                var created = await documentService_.UploadAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.DocumentId }, created);
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

        [HttpGet("{id:int}/download-url")]
        public async Task<ActionResult> GetDownloadUrl(int id)
        {
            var document = await documentService_.GetByIdAsync(id);
            if (document is null || !await CanAccessClientAsync(document.ClientId))
            {
                return NotFound(new { message = $"Document {id} was not found" });
            }

            // A client should never get a download link for a document the
            // adviser has marked internal-only, even though they might see
            // its existence via GetForClient - only GetVisibleToClient
            // filters that, and a client could otherwise call this
            // endpoint directly with a guessed/enumerated id.
            if (!IsAdvisor && !document.VisibleToClient)
            {
                return NotFound(new { message = $"Document {id} was not found" });
            }

            try
            {
                var uri = await documentService_.GetDownloadUriAsync(id);
                return Ok(new { url = uri.ToString() });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}/visibility")]
        [Authorize(Roles = AppRoles.Advisor)] // Only the adviser controls what a client can see.
        public async Task<ActionResult<DocumentDto>> SetVisibility(int id, [FromBody] UpdateDocumentVisibilityRequest request)
        {
            var document = await documentService_.GetByIdAsync(id);
            if (document is null || !await CanAccessClientAsync(document.ClientId))
            {
                return NotFound(new { message = $"Document {id} was not found." });
            }

            try
            {
                return Ok(await documentService_.SetVisibilityAsync(id, request.VisibleToClient));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await documentService_.GetByIdAsync(id);
            if (document is null || !await CanAccessClientAsync(document.ClientId))
            {
                return NotFound(new { message = $"Document {id} was not found" });
            }

            var deleted = await documentService_.DeleteAsync(id);
            return deleted ? NoContent() : NotFound(new { message = $"Document {id} was not found" });
        }
    }
}
