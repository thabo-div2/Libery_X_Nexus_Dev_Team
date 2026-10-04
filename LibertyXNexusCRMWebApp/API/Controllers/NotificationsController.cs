using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Handles the advisor's notifications.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Advisor)]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        /// <summary>
        /// Sets up the controller.
        /// </summary>
        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Gets the advisor id from the token.
        /// </summary>
        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        /// <summary>
        /// Gets the advisor's notifications.
        /// </summary>
        [HttpGet("advisor/{advisorId:int}")]
        public async Task<IActionResult> GetFeed(int advisorId)
        {
            if (advisorId != CurrentAdvisorId)
            {
                return Forbid();
            }

            return Ok(await _notificationService.GetFeedForAdvisorAsync(advisorId));
        }

        /// <summary>
        /// Gets the unread notification count.
        /// </summary>
        [HttpGet("advisor/{advisorId:int}/unread-count")]
        public async Task<IActionResult> GetUnreadCount(int advisorId)
        {
            if (advisorId != CurrentAdvisorId)
            {
                return Forbid();
            }

            return Ok(await _notificationService.GetUnreadCountForAdvisorAsync(advisorId));
        }

        /// <summary>
        /// Marks one notification as read.
        /// </summary>
        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var updated = await _notificationService.MarkAsReadForAdvisorAsync(id, CurrentAdvisorId);
            return updated ? NoContent() : NotFound();
        }

        /// <summary>
        /// Marks all notifications as read.
        /// </summary>
        [HttpPut("advisor/{advisorId:int}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(int advisorId)
        {
            if (advisorId != CurrentAdvisorId)
            {
                return Forbid();
            }
            await _notificationService.MarkAllAsReadForAdvisorAsync(advisorId);
            return NoContent();
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
