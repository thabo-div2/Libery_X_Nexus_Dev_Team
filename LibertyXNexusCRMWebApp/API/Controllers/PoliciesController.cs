using API.DTOs.Policies;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService policyService_;

        public PoliciesController(IPolicyService policyService)
        {
            policyService_ = policyService;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PolicyDto>> GetById(int id)
        {
            var policy = await policyService_.GetByIdAsync(id);
            return policy is null ? NotFound(new {message = $"Policy {id} was not found."}) : Ok(policy);
        }

        [HttpGet("{id:int}/with-documents")]
        public async Task<ActionResult<PolicyDto>> GetWithDocuments(int id)
        {
            var policy = await policyService_.GetWithDocumentsAsync(id);
            return policy is null ? NotFound(new { message = $"Policy {id} was not found." }) : Ok(policy);
        }

        [HttpGet("catalogue")]
        public async Task<ActionResult<IEnumerable<PolicyDto>>> GetCatalogue()
        {
            return Ok(await policyService_.GetCatalogueAsync());
        }

        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<PolicyDto>>> GetForClient(int clientId)
        {
            return Ok(await policyService_.GetForClientAsync(clientId));
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<PolicyDto>>> GetByStatus(PolicyStatus status)
        {
            return Ok(await policyService_.GetByStatusAsync(status));
        }

        [HttpPost("catalogue")]
        public async Task<ActionResult<PolicyDto>> CreateCatalogueItem([FromBody] CreateCataloguePolicyRequest request)
        {
            var created = await policyService_.CreateCatalogueItemAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.PolicyId }, created);
        }

        [HttpPost("client/{clientId:int}")]
        public async Task<ActionResult<PolicyDto>> CreateClientPolicy(int clientId, [FromBody] CreateClientPolicyRequest request)
        {
            try
            {
                var created = await policyService_.CreateClientPolicyAsync(clientId, request);
                return CreatedAtAction(nameof(GetById), new {id = created.PolicyId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new {message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new {message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<PolicyDto>> Update(int id, [FromBody] UpdatePolicyRequest request)
        {
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
        public async Task<ActionResult<PolicyDto>> UpdateStatus(int id, [FromBody] UpdatePolicyStatusRequest request)
        {
            try
            {
                return Ok(await policyService_.UpdateStatusAsync(id, request));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new {message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new {message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await policyService_.DeleteAsync(id);
            return deleted ? NoContent() : NotFound(new {message = $"Policy {id} was not found." });
        }
    }
}