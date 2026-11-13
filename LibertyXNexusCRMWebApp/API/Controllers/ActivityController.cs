using API.DTOs.Activity;
using API.Identity;
using API.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// The activity log: a record of what the signed-in advisor changed and when.
    /// An advisor only ever sees their own entries.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Advisor)]
    public class ActivityController : ControllerBase
    {
        private const int MaxCount = 500;

        private readonly IAuditLogRepository _auditLogRepository;

        public ActivityController(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        /// <summary>
        /// Gets the advisor's most recent activity, newest first.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActivityItemDto>>> GetMine([FromQuery] int count = 200)
        {
            count = Math.Clamp(count, 1, MaxCount);

            var entries = await _auditLogRepository.GetRecentForUserAsync(CurrentAdvisorId, UserRole.FinancialAdviser, count);

            return Ok(entries.Select(a => new ActivityItemDto
            {
                Timestamp = a.Timestamp,
                Action = a.ActionType.ToString(),
                Entity = a.EntityAffected,
                EntityId = a.EntityId,
                Details = a.Details
            }).ToList());
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
