using API.DTOs.Policies;
using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService policyService_;
        private readonly IClientService _clientService;

        public PoliciesController(IPolicyService policyService, IClientService clientService)
        {
            policyService_ = policyService;
            _clientService = clientService;
        }

        private bool IsAdvisor => User.IsInRole(AppRoles.Advisor);

        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        private int CurrentClientId => int.TryParse(User.FindFirstValue("clientId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no clientId claim.");

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

        /// <summary>Public catalogue - any authenticated user (client or adviser) can browse it.</summary>
        [HttpGet("catalogue")]
        public async Task<ActionResult<IEnumerable<PolicyDto>>> GetCatalogue()
        {
            return Ok(await policyService_.GetCatalogueAsync());
        }

        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<PolicyDto>>> GetForClient(int clientId)
        {
            if (!await CanAccessClientAsync(clientId)) return Forbid();
            return Ok(await policyService_.GetForClientAsync(clientId));
        }

        /// <summary>Advisor-only: cross-client view with no per-client scoping in the URL.</summary>
        [HttpGet("status/{status}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<PolicyDto>>> GetByStatus(PolicyStatus status)
        {
            return Ok(await policyService_.GetByStatusAsync(status));
        }

        /// <summary>Advisor-only - only the adviser manages what's in the public catalogue.</summary>
        [HttpPost("catalogue")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<PolicyDto>> CreateCatalogueItem([FromBody] CreateCataloguePolicyRequest request)
        {
            var created = await policyService_.CreateCatalogueItemAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.PolicyId }, created);
        }

        [HttpPost("client/{clientId:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<PolicyDto>> CreateClientPolicy(int clientId, [FromBody] CreateClientPolicyRequest request)
        {
            if (!await CanAccessClientAsync(clientId)) return Forbid();

            try
            {
                var created = await policyService_.CreateClientPolicyAsync(clientId, request);
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
                return updated is null ? NotFound(new { message = $"Policy {id} was not found." }) : Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

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
                return Ok(await policyService_.UpdateStatusAsync(id, request));
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

            var deleted = await policyService_.DeleteAsync(id);
            return deleted ? NoContent() : NotFound(new { message = $"Policy {id} was not found." });
        }
    }
}