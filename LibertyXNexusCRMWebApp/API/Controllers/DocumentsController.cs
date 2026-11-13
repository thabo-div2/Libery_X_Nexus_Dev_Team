using API.Security;
using Microsoft.AspNetCore.RateLimiting;
using API.DTOs.Documents;
using API.Identity;
using API.Repositories.Interfaces;
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
        private readonly INotificationService _notificationService;
        private readonly IDocumentRequestService _documentRequestService;
        private readonly IAuditLogRepository _auditLogRepository;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentsController"/> class with the specified document and client services.
        /// </summary>
        /// <param name="documentService"></param>
        /// <param name="clientService"></param>
        /// <param name="policyService"></param>
        /// <param name="notificationService"></param>
        public DocumentsController(IDocumentService documentService, IClientService clientService, IPolicyService policyService, INotificationService notificationService, IDocumentRequestService documentRequestService, IAuditLogRepository auditLogRepository)
        {
            documentService_ = documentService;
            _clientService = clientService;
            _policyService = policyService;
            _notificationService = notificationService;
            _documentRequestService = documentRequestService;
            _auditLogRepository = auditLogRepository;
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
        /// Lets a client know their advisor shared a document with them.
        /// </summary>
        private Task NotifyClientOfSharedDocumentAsync(int clientId, string fileName)
        {
            return _notificationService.NotifyClientAsync(
                clientId,
                NotificationType.DocumentUploaded,
                $"Your advisor shared a document with you: {fileName}.",
                "/client-documents");
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Records what was done to a document in the activity log.
        /// Logging must never break the request, so any failure here is ignored.
        /// </summary>
        private async Task LogAsync(AuditActionType action, int documentId, string details)
        {
            try
            {
                await _auditLogRepository.LogAsync(
                    userId: IsAdvisor ? CurrentAdvisorId : CurrentClientId,
                    userRole: IsAdvisor ? UserRole.FinancialAdviser : UserRole.RegisteredClient,
                    actionType: action,
                    entityAffected: "Document",
                    entityId: documentId,
                    details: details);
            }
            catch (Exception)
            {
                // Ignore - the action itself worked.
            }
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
            if (document is null
                 || !await CanAccessClientAsync(document.ClientId)
                 || (!IsAdvisor && !document.VisibleToClient))
            {
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

            var documents = await documentService_.GetForClientAsync(clientId);
            if (!IsAdvisor)
            {
                documents = documents.Where(d => d.VisibleToClient);
            }
            return Ok(documents);
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

            if (policy.ClientId is null)
            {
                return NotFound(new { message = "Policy is not linked to a client." });
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
            return Ok(await documentService_.GetByTypeAsync(documentType, CurrentAdvisorId));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Uploads a new document for a specific client.
        /// Advisors can upload for any of their own clients and choose whether the client can see it.
        /// Clients can only upload for themselves; their uploads are always visible to them, and if they
        /// choose General documents, it is saved without a policy. The advisor gets a notification.
        /// The uploaded file must be sent as multipart/form-data, and the request size limit is set to 30 MB.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = AppRoles.Advisor + "," + AppRoles.Client)]
        [EnableRateLimiting(RateLimitPolicies.Upload)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(30_000_000)]
        public async Task<ActionResult<DocumentDto>> Upload([FromForm] UploadDocumentRequest request)
        {
            var client = await _clientService.GetByIdAsync(request.ClientId);

            if (IsAdvisor)
            {
                if (client is null || client.AdvisorId != CurrentAdvisorId)
                {
                    return Forbid();
                }

                var uploaderName = $"{User.FindFirstValue("firstName")} {User.FindFirstValue("lastName")}".Trim();
                request.UploadedBy = string.IsNullOrWhiteSpace(uploaderName)
                    ? $"advisor-{CurrentAdvisorId}"
                    : uploaderName;
            }
            else
            {
                // A client can only ever upload to their own profile.
                if (client is null || request.ClientId != CurrentClientId)
                {
                    return Forbid();
                }

                // Clients should always be able to see what they uploaded themselves.
                request.VisibleToClient = true;

                var clientName = $"{client.FirstName} {client.LastName}".Trim();
                request.UploadedBy = string.IsNullOrWhiteSpace(clientName)
                    ? $"client-{request.ClientId}"
                    : clientName;
            }

            DocumentDto created;
            try
            {
                created = await documentService_.UploadAsync(request);
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

            // The repository saves the document and completes its matching request in one transaction.

            // Tell the other side about the new document. The file is already saved,
            // so a failed notification shouldn't turn the upload into an error.
            try
            {
                if (!IsAdvisor && client.AdvisorId is int advisorId)
                {
                    await _notificationService.NotifyAdvisorAsync(
                        advisorId,
                        NotificationType.DocumentUploaded,
                        $"{request.UploadedBy} uploaded a document: {created.FileName}.",
                        "/documents",
                        request.ClientId);
                }
                else if (IsAdvisor && request.VisibleToClient)
                {
                    await NotifyClientOfSharedDocumentAsync(request.ClientId, created.FileName);
                }
            }
            catch (Exception)
            {
                // Ignore - the upload itself worked.
            }

            await LogAsync(AuditActionType.DocumentUpload, created.DocumentId, $"Uploaded {created.FileName} for client {request.ClientId}.");

            return CreatedAtAction(nameof(GetById), new { id = created.DocumentId }, created);
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
                await LogAsync(AuditActionType.DocumentDownload, id, $"Downloaded {document.FileName} (client {document.ClientId}).");

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

            DocumentDto updated;
            try
            {
                updated = await documentService_.SetVisibilityAsync(id, request.VisibleToClient);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }

            // Only tell the client when a hidden document becomes visible, not every time it's saved.
            if (!document.VisibleToClient && request.VisibleToClient)
            {
                try
                {
                    await NotifyClientOfSharedDocumentAsync(document.ClientId, document.FileName);
                }
                catch (Exception)
                {
                    // Ignore - the visibility change itself worked.
                }
            }

            await LogAsync(
                AuditActionType.PermissionChange,
                id,
                request.VisibleToClient
                    ? $"Shared {document.FileName} with client {document.ClientId}."
                    : $"Hid {document.FileName} from client {document.ClientId}.");

            return Ok(updated);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Sets a document's signature status. The advisor asks for a signature (Awaiting), signs it off
        /// once the signed copy is back (Signed), or clears the request (NotRequired).
        /// The client is told when a signature is asked for on a document they can see.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpPut("{id:int}/signature/{status}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<DocumentDto>> SetSignatureStatus(int id, SignatureStatus status)
        {
            if (!Enum.IsDefined(status))
            {
                return BadRequest(new { message = "That signature status is not recognised." });
            }

            var document = await documentService_.GetByIdAsync(id);
            if (document is null || !await CanAccessClientAsync(document.ClientId))
            {
                return NotFound(new { message = $"Document {id} was not found." });
            }

            DocumentDto updated;
            try
            {
                updated = await documentService_.SetSignatureStatusAsync(id, status);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }

            // Only tell the client when a signature is newly asked for, and only if they can open the document.
            if (status == SignatureStatus.Awaiting
                && document.SignatureStatus != nameof(SignatureStatus.Awaiting)
                && document.VisibleToClient)
            {
                try
                {
                    await _notificationService.NotifyClientAsync(
                        document.ClientId,
                        NotificationType.PendingAction,
                        $"Your advisor needs your signature on {document.FileName}.",
                        "/client-documents");
                }
                catch (Exception)
                {
                    // Ignore - the status change itself worked.
                }
            }

            await LogAsync(AuditActionType.Update, id, $"Set the signature status of {document.FileName} to {status}.");

            return Ok(updated);
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

            if (deleted)
            {
                await LogAsync(AuditActionType.Delete, id, $"Deleted {document.FileName} (client {document.ClientId}).");
            }

            return deleted ? NoContent() : NotFound(new { message = $"Document {id} was not found" });
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
