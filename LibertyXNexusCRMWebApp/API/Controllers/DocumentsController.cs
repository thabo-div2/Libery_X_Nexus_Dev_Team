using API.DTOs.Documents;
using API.Identity;
using API.Services.Implementations;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing documents. Provides endpoints for advisors to upload, retrieve, update visibility, and delete documents associated with clients and policies. Clients can also retrieve documents that are visible to them.
    /// </summary>
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService documentService_;
        private readonly IClientService _clientService;
        private readonly IPolicyService _policyService;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentsController"/> class with the specified document and client services.
        /// </summary>
        /// <param name="documentService"></param>
        /// <param name="clientService"></param>
        /// <param name="policyService"></param>
        public DocumentsController(IDocumentService documentService, IClientService clientService, IPolicyService policyService)
        {
            documentService_ = documentService;
            _clientService = clientService;
            _policyService = policyService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets a value indicating whether the current user is an advisor based on their role in the authentication token.
        /// </summary>
        private bool IsAdvisor => User.IsInRole(AppRoles.Advisor);

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current advisor's ID from their login token so they can only access their own records.
        /// </summary>
        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current client's ID from their login token so they can only access their own records.
        /// </summary>
        private int CurrentClientId => int.TryParse(User.FindFirstValue("clientId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no clientId claim.");

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Checks if the current user can access the specified client based on their role and ownership. Advisors can access clients they own, while clients can only access their own records.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
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
        /// Gets a document by its ID. Returns 404 if the document does not exist or if the current user does not have access to it.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Advisor can fetch all the documents.
        /// </summary>
        /// <param name="advisorId"></param>
        /// <returns></returns>
        [HttpGet("advisor/{advisorId:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetAllDocuments(int advisorId)
        {
            // Only allow an advisor to read their own documents.
            if (!int.TryParse(User.FindFirstValue("advisorId"), out var tokenAdvisorId)
                || tokenAdvisorId != advisorId)
            {
                return Forbid();
            }

            var documents = await documentService_.GetAllDocumentsAsync(advisorId);
            return Ok(documents);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all documents for a specific client. Returns 403 if the current user does not have access to the specified client.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetForClient(int clientId)
        {
            if (!await CanAccessClientAsync(clientId)) return Forbid();
            return Ok(await documentService_.GetForClientAsync(clientId));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all documents that are visible to a specific client. Returns 403 if the current user does not have access to the specified client.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [HttpGet("client/{clientId:int}/visible")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetVisibleToClient(int clientId)
        {
            if (!await CanAccessClientAsync(clientId)) return Forbid();
            return Ok(await documentService_.GetVisibleToClientAsync(clientId));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all documents for a specific policy. This endpoint is restricted to advisors only, as it returns documents across all clients without per-client scoping.
        /// </summary>
        /// <param name="policyId"></param>
        /// <returns></returns>
        [HttpGet("policy/{policyId:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetForPolicy(int policyId)
        {
            var policy = await _policyService.GetByIdAsync(policyId);

            if (policy is null)
            {
                return NotFound(new
                {
                    message = "Policy was not found."
                });
            }

            var client = await _clientService.GetByIdAsync(policy.ClientId.Value);

            if (client is null || client.AdvisorId != CurrentAdvisorId)
            {
                // Do not reveal that another advisor's policy exists.
                return NotFound(new
                {
                    message = "Policy was not found."
                });
            }

            return Ok(await documentService_.GetForPolicyAsync(policyId));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all documents of a specific type. This endpoint is restricted to advisors only, as it returns documents across all clients without per-client scoping.
        /// </summary>
        /// <param name="documentType"></param>
        /// <returns></returns>
        [HttpGet("type/{documentType}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetByType(DocumentType documentType)
        {
            return Ok(await documentService_.GetByTypeAsync(documentType));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Uploads a new document for a specific client. 
        /// This endpoint is restricted to advisors only, and the uploaded document will be associated with the specified client and policy. 
        /// The uploaded file must be sent as multipart/form-data, and the request size limit is set to 30 MB.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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

            // Set the UploadedBy field to the current user's name or email, or a fallback identifier if neither is available.
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets a temporary download URL for a specific document. 
        /// This endpoint checks if the current user has access to the document and whether the document is visible to the client. 
        /// If the user is not an advisor and the document is not visible to the client, a 404 response is returned to avoid revealing the existence of the document.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}/download-url")]
        public async Task<ActionResult> GetDownloadUrl(int id)
        {
            var document = await documentService_.GetByIdAsync(id);
            if (document is null || !await CanAccessClientAsync(document.ClientId))
            {
                return NotFound(new { message = $"Document {id} was not found" });
            }

            // If the user is not an advisor and the document is not visible to the client, return 404 to avoid revealing the existence of the document.
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Updates the visibility setting for a specific document.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:int}/visibility")]
        [Authorize(Roles = AppRoles.Advisor)]
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes a specific document. 
        /// This endpoint is restricted to advisors only, and it checks if the current user has access to the document before attempting deletion. 
        /// If the document does not exist or the user does not have access, a 404 response is returned.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
