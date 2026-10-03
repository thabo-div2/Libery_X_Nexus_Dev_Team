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
    /// <summary>
    /// Controller for managing clients. Provides endpoints for advisors to create, read, update, and delete client records, as well as for clients to view their own profile.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly IAuditLogRepository _auditLogRepository;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientsController"/> class with the specified client service and audit log repository.
        /// </summary>
        /// <param name="clientService"></param>
        /// <param name="auditLogRepository"></param>
        public ClientsController(IClientService clientService, IAuditLogRepository auditLogRepository)
        {
            _clientService = clientService;
            _auditLogRepository = auditLogRepository;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current advisor's ID from their login token so they can only access their own records.
        /// </summary>
        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId Claim. Was it issued before this claim existed?");

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current client's ID from their login token so they can only access their own records.
        /// </summary>
        private int CurrentClientId => int.TryParse(User.FindFirstValue("clientId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no clientId claim.");


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Searches for clients based on the provided search term and status. Only accessible by advisors.
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<ClientListItemDto>>> Search([FromQuery] string? searchTerm, [FromQuery] ClientStatus? status)
        {
            var results = await _clientService.SearchAsync(searchTerm, status, CurrentAdvisorId);
            return Ok(results);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the details of a specific client by their ID. Accessible by both advisors and clients, but with restrictions based on the user's role.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ClientDetailDto>> GetById(int id)
        {
            var client = await _clientService.GetByIdAsync(id);

            if (client is null)
            {
                return NotFound(new { message = $"Client {id} was not found" });
            }

            var role = User.FindFirstValue("role");

            if (role == AppRoles.Client)
            {
                // Clients may only retrieve their own profile.
                if (id != CurrentClientId)
                    return Forbid();

                return Ok(client);
            }

            if (role == AppRoles.Advisor)
            {
                // Advisors may only retrieve clients assigned to them.
                if (client.AdvisorId != CurrentAdvisorId)
                    return NotFound(new { message = $"Client {id} was not found" });

                return Ok(client);
            }

            return Forbid();
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Creates a new client record. Only accessible by advisors. Logs the creation action in the audit log.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<ClientDetailDto>> Create([FromBody] CreateClientRequest request)
        {
            request.AdvisorId = CurrentAdvisorId;

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
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Updates an existing client record. Only accessible by advisors. Logs the update action in the audit log.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes an existing client record. Only accessible by advisors. Logs the deletion action in the audit log.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
