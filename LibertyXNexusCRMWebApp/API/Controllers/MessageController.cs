using API.DTOs.Messages;
using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        private int CurrentClientId => int.TryParse(User.FindFirstValue("clientId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no clientId claim.");

        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetConversation(int clientId)
        {
            var role = User.FindFirstValue("role");

            if (role == AppRoles.Client)
            {
                if (clientId != CurrentClientId)
                    return Forbid();

                return Ok(await _messageService.GetConversationForClientAsync(clientId));
            }

            if (role == AppRoles.Advisor)
            {
                return Ok(await _messageService.GetConversationForAdvisorAsync(CurrentAdvisorId, clientId));
            }

            return Forbid();
        }

        [HttpGet("advisor/{advisorId:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<ConversationSummaryDto>>> GetForAdvisor(int advisorId)
        {
            if (advisorId != CurrentAdvisorId)
                return Forbid();

            return Ok(await _messageService.GetConversationsForAdvisorAsync(advisorId));
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> Send([FromBody] SendMessageRequest request)
        {
            var role = User.FindFirstValue("role");
            var fromAdvisor = role == AppRoles.Advisor;

            if (!fromAdvisor && role != AppRoles.Client)
                return Forbid();

            if (fromAdvisor && request.AdvisorId != CurrentAdvisorId)
                return Forbid();

            if (!fromAdvisor && request.ClientId != CurrentClientId)
                return Forbid();

            try
            {
                var created = await _messageService.SendAsync(request, fromAdvisor);
                return Created($"/api/Message/client/{created.ClientId}", created);
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
    }
}
