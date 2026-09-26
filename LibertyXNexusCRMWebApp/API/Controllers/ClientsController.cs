using API.DTOs.Clients;
using API.Identity;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Advisor)]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly IAuditLogRepository _auditLogRepository;
        public ClientsController(IClientService clientService, IAuditLogRepository auditLogRepository)
        {
            _clientService = clientService;
            _auditLogRepository = auditLogRepository;
        }
        /// <summary>
        /// Gets the current advisor's ID from their login token so they can only access their own records.
        /// </summary>
        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId Claim. Was it issued before this claim existed?");

        [HttpGet]
        public async Task <ActionResult<IEnumerable<ClientListItemDto>>> Search([FromQuery] string? searchTerm, [FromQuery] ClientStatus? status)
        {
            var results = await _clientService.SearchAsync(searchTerm, status, CurrentAdvisorId);
            return Ok(results);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ClientDetailDto>> GetById(int id)
        {
            var client = await _clientService.GetByIdAsync(id);
            if (client is null || client.AdvisorId != CurrentAdvisorId)
            {
                return NotFound(new { message = $"Client {id} was not found" });
            }
            return Ok(client);
        }

        [HttpPost]
        public async Task<ActionResult<ClientDetailDto>> Create([FromBody] CreateClientRequest request)
        {
            try
            {
                var created = await _clientService.CreateAsync(request);

                await _auditLogRepository.LogAsync(
                    userId: CurrentAdvisorId,
                    userRole: UserRole.FinancialAdviser,
                    actionType: AuditActionType.Create,
                    entityAffected: "Client",
                    details: $"Advisor created client {created.ClientId}");

                return CreatedAtAction(nameof(GetById), new { id = created.ClientId }, created);

            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ClientDetailDto>> Update(int id, [FromBody] UpdateClientRequest request)
        {
            var existing = await _clientService.GetByIdAsync(id);

            if (existing is null || existing.AdvisorId != CurrentAdvisorId)
            {
                return NotFound(new { message = $"Client {id} was not found" });
            }

            var updated = await _clientService.UpdateAsync(id, request);

            await _auditLogRepository.LogAsync(
                userId: CurrentAdvisorId,
                userRole: UserRole.FinancialAdviser,
                actionType: AuditActionType.Update,
                entityAffected: "Client",
                details: $"Advisor updated client {id}.");

            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var existing = await _clientService.GetByIdAsync(id);

            if (existing is null || existing.AdvisorId != CurrentAdvisorId)
            {
                return NotFound(new { message = $"Client {id} was not found" });
            }

            await _clientService.DeleteAsync(id);

            await _auditLogRepository.LogAsync(
                userId: CurrentAdvisorId,
                userRole: UserRole.FinancialAdviser,
                actionType: AuditActionType.Delete,
                entityAffected: "Client",
                details: $"Advisor deleted client {id}.");

            return NoContent();
        }
    }
}
