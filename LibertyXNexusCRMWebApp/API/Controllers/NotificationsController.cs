using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// This controller handles the advisor's notifications, like the ones that show up under the bell icon.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Advisor)]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        /// <summary>
        /// Sets up the controller with the notification service.
        /// </summary>
        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Gets the advisor's id from the logged in user's token.
        /// </summary>
        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        /// <summary>
        /// Gets all the notifications for the logged in advisor.
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
        /// Gets how many notifications the advisor hasn't read yet.
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
        /// Marks all of the advisor's notifications as read.
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
