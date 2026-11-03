using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Handles advisor and client notifications. Each person can only see and change their own.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
        /// Gets the client id from the token.
        /// </summary>
        private int CurrentClientId => int.TryParse(User.FindFirstValue("clientId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no clientId claim.");

        private bool IsAdvisor => User.IsInRole(AppRoles.Advisor);

        /// <summary>
        /// Gets the advisor's notifications.
        /// </summary>
        [HttpGet("advisor/{advisorId:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
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
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<IActionResult> GetUnreadCount(int advisorId)
        {
            if (advisorId != CurrentAdvisorId)
            {
                return Forbid();
            }

            return Ok(await _notificationService.GetUnreadCountForAdvisorAsync(advisorId));
        }

        /// <summary>
        /// Marks one notification as read, for whichever advisor or client is signed in.
        /// Returns 204 when it worked, or 404 with a JSON message when the notification
        /// doesn't exist or belongs to someone else (the same message either way,
        /// so it doesn't reveal that someone else's notification exists).
        /// </summary>
        [HttpPut("{id:int}/read")]
        [Authorize(Roles = AppRoles.Advisor + "," + AppRoles.Client)]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var updated = IsAdvisor
                ? await _notificationService.MarkAsReadForAdvisorAsync(id, CurrentAdvisorId)
                : await _notificationService.MarkAsReadForClientAsync(id, CurrentClientId);

            if (!updated)
            {
                return NotFound(new { message = "Notification not found." });
            }

            return NoContent();
        }

        /// <summary>
        /// Marks all notifications as read.
        /// </summary>
        [HttpPut("advisor/{advisorId:int}/read-all")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<IActionResult> MarkAllAsRead(int advisorId)
        {
            if (advisorId != CurrentAdvisorId)
            {
                return Forbid();
            }
            await _notificationService.MarkAllAsReadForAdvisorAsync(advisorId);
            return NoContent();
        }

        /// <summary>
        /// Gets the signed-in client's notifications.
        /// </summary>
        [HttpGet("client/{clientId:int}")]
        [Authorize(Roles = AppRoles.Client)]
        public async Task<IActionResult> GetClientFeed(int clientId)
        {
            if (clientId != CurrentClientId)
            {
                return Forbid();
            }

            return Ok(await _notificationService.GetFeedForClientAsync(clientId));
        }

        /// <summary>
        /// Gets the signed-in client's unread notification count.
        /// </summary>
        [HttpGet("client/{clientId:int}/unread-count")]
        [Authorize(Roles = AppRoles.Client)]
        public async Task<IActionResult> GetClientUnreadCount(int clientId)
        {
            if (clientId != CurrentClientId)
            {
                return Forbid();
            }

            return Ok(await _notificationService.GetUnreadCountForClientAsync(clientId));
        }

        /// <summary>
        /// Marks all of the signed-in client's notifications as read.
        /// </summary>
        [HttpPut("client/{clientId:int}/read-all")]
        [Authorize(Roles = AppRoles.Client)]
        public async Task<IActionResult> MarkAllClientAsRead(int clientId)
        {
            if (clientId != CurrentClientId)
            {
                return Forbid();
            }

            await _notificationService.MarkAllAsReadForClientAsync(clientId);
            return NoContent();
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
