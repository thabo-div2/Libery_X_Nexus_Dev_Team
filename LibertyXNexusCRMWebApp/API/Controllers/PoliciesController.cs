using API.DTOs.Policies;
using API.Identity;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing policies. Provides endpoints for advisors and clients to create, read, update, and delete policy records, as well as to access the public policy catalogue.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService policyService_;
        private readonly IClientService _clientService;
        private readonly IAuditLogRepository _auditLogRepository;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="PoliciesController"/> class with the specified policy service and client service.
        /// </summary>
        /// <param name="policyService"></param>
        /// <param name="clientService"></param>
        public PoliciesController(IPolicyService policyService, IClientService clientService, IAuditLogRepository auditLogRepository)
        {
            policyService_ = policyService;
            _clientService = clientService;
            _auditLogRepository = auditLogRepository;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets a value indicating whether the current user is an advisor based on their role claim. This property is used to determine access permissions for certain operations.
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
        /// Records a policy change in the activity log. Logging must never break the request.
        /// </summary>
        private async Task LogAsync(AuditActionType action, int policyId, string details)
        {
            try
            {
                await _auditLogRepository.LogAsync(
                    userId: CurrentAdvisorId,
                    userRole: UserRole.FinancialAdviser,
                    actionType: action,
                    entityAffected: "Policy",
                    entityId: policyId,
                    details: details);
            }
            catch (Exception)
            {
                // Ignore - the action itself worked.
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Checks if the current user can access the specified client's records. 
        /// Advisors can access their own clients, while clients can only access their own records. 
        /// Returns true if access is allowed; otherwise, false.
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
        /// Gets a policy by its ID. 
        /// If the policy is a catalogue item (ClientId is null), it is visible to any authenticated user. 
        /// If the policy belongs to a client, access is restricted to the client or their advisor. 
        /// Returns 404 if the policy is not found or access is denied.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PolicyDto>> GetById(int id)
        {
            var policy = await policyService_.GetByIdAsync(id);
            if (policy is null) return NotFound(new { message = $"Policy {id} was not found." });

            // Catalogue items (ClientId == null) are visible to anyone
            // authenticated - that's the point of a public catalogue. A
            // client-held policy needs an ownership check.
            if (policy.ClientId.HasValue && !await CanAccessClientAsync(policy.ClientId.Value))
            {
                return NotFound(new { message = $"Policy {id} was not found." });
            }

            return Ok(policy);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets a policy by its ID, including any associated documents.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}/with-documents")]
        public async Task<ActionResult<PolicyDto>> GetWithDocuments(int id)
        {
            var policy = await policyService_.GetWithDocumentsAsync(id);
            if (policy is null) return NotFound(new { message = $"Policy {id} was not found." });

            if (policy.ClientId.HasValue && !await CanAccessClientAsync(policy.ClientId.Value))
            {
                return NotFound(new { message = $"Policy {id} was not found." });
            }

            return Ok(policy);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the public policy catalogue, which includes all policies that are not associated with a specific client (ClientId is null). This endpoint is accessible to any authenticated user.
        /// </summary>
        /// <returns></returns>
        [HttpGet("catalogue")]
        public async Task<ActionResult<IEnumerable<PolicyDto>>> GetCatalogue()
        {
            return Ok(await policyService_.GetCatalogueAsync());
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all policies associated with a specific client. Access is restricted to the client themselves or their advisor. Returns 403 if access is denied.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<PolicyDto>>> GetForClient(int clientId)
        {
            if (!await CanAccessClientAsync(clientId)) return Forbid();
            return Ok(await policyService_.GetForClientAsync(clientId));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all policies with the specified status. This endpoint is restricted to advisors only, as they manage the policies in the system. Returns 403 if the user is not an advisor.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet("status/{status}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<PolicyDto>>> GetByStatus(PolicyStatus status)
        {
            return Ok(await policyService_.GetByStatusAsync(status, CurrentAdvisorId));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Creates a new policy in the public catalogue. This endpoint is restricted to advisors only, as they manage the policies in the system. Returns 403 if the user is not an advisor.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("catalogue")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<PolicyDto>> CreateCatalogueItem([FromBody] CreateCataloguePolicyRequest request)
        {
            var created = await policyService_.CreateCatalogueItemAsync(request);
            await LogAsync(AuditActionType.Create, created.PolicyId, $"Added catalogue policy {created.PolicyName}.");
            return CreatedAtAction(nameof(GetById), new { id = created.PolicyId }, created);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Creates a new policy for a specific client. Access is restricted to the client themselves or their advisor. Returns 403 if access is denied.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("client/{clientId:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<PolicyDto>> CreateClientPolicy(int clientId, [FromBody] CreateClientPolicyRequest request)
        {
            if (!await CanAccessClientAsync(clientId)) return Forbid();

            try
            {
                var created = await policyService_.CreateClientPolicyAsync(clientId, request);
                await LogAsync(AuditActionType.Create, created.PolicyId, $"Added policy {created.PolicyName} to client {clientId}.");
                return CreatedAtAction(nameof(GetById), new { id = created.PolicyId }, created);
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
        /// Updates an existing policy by its ID. Access is restricted to the client themselves or their advisor. Returns 404 if the policy is not found or access is denied, and 400 if the request data is invalid.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<PolicyDto>> Update(int id, [FromBody] UpdatePolicyRequest request)
        {
            var existing = await policyService_.GetByIdAsync(id);
            if (existing is null) return NotFound(new { message = $"Policy {id} was not found." });
            if (existing.ClientId.HasValue && !await CanAccessClientAsync(existing.ClientId.Value))
            {
                return NotFound(new { message = $"Policy {id} was not found." });
            }

            try
            {
                var updated = await policyService_.UpdateAsync(id, request);
                if (updated is not null)
                {
                    await LogAsync(AuditActionType.Update, id, $"Updated policy {updated.PolicyName}.");
                }
                return updated is null ? NotFound(new { message = $"Policy {id} was not found." }) : Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Updates the status of an existing policy by its ID. Access is restricted to advisors only, as they manage the policies in the system. Returns 404 if the policy is not found or access is denied, and 409 if the status update is invalid due to business rules.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<PolicyDto>> UpdateStatus(int id, [FromBody] UpdatePolicyStatusRequest request)
        {
            var existing = await policyService_.GetByIdAsync(id);
            if (existing is null) return NotFound(new { message = $"Policy {id} was not found." });
            if (existing.ClientId.HasValue && !await CanAccessClientAsync(existing.ClientId.Value))
            {
                return NotFound(new { message = $"Policy {id} was not found." });
            }

            try
            {
                var updated = await policyService_.UpdateStatusAsync(id, request);
                if (existing.Status != updated.Status)
                    await LogAsync(AuditActionType.Update, id, $"Changed policy {updated.PolicyName} from {existing.Status} to {updated.Status}.");
                return Ok(updated);
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
        /// Deletes an existing policy by its ID. Access is restricted to advisors only, as they manage the policies in the system. Returns 404 if the policy is not found or access is denied.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await policyService_.GetByIdAsync(id);
            if (existing is null) return NotFound(new { message = $"Policy {id} was not found." });
            if (existing.ClientId.HasValue && !await CanAccessClientAsync(existing.ClientId.Value))
            {
                return NotFound(new { message = $"Policy {id} was not found." });
            }

            try
            {
                var deleted = await policyService_.DeleteAsync(id);

                if (deleted)
                {
                    await LogAsync(AuditActionType.Delete, id, $"Removed policy {existing.PolicyName}.");
                }

                return deleted ? NoContent() : NotFound(new { message = $"Policy {id} was not found." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
