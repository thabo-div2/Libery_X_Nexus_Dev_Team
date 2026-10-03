using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Advisor)]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        [HttpGet("advisor/{advisorId:int}")]
        public async Task<IActionResult> GetFeed(int advisorId)
        {
            if (advisorId != CurrentAdvisorId)
            {
                return Forbid();
            }

            return Ok(await _notificationService.GetFeedForAdvisorAsync(advisorId));
        }

        [HttpGet("advisor/{advisorId:int}/unread-count")]
        public async Task<IActionResult> GetUnreadCount(int advisorId)
        {
            if (advisorId != CurrentAdvisorId)
            {
                return Forbid();
            }

            return Ok(await _notificationService.GetUnreadCountForAdvisorAsync(advisorId));
        }

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var updated = await _notificationService.MarkAsReadForAdvisorAsync(id, CurrentAdvisorId);
            return updated ? NoContent() : NotFound();
        }

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
