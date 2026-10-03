using API.DTOs.Messages;
using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing messages between clients and advisors. Provides endpoints for retrieving conversations, sending messages, and getting conversation summaries.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageController"/> class with the specified message service.
        /// </summary>
        /// <param name="messageService"></param>
        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current client's ID from their login token so they can only access their own records.
        /// </summary>
        private int CurrentClientId => int.TryParse(User.FindFirstValue("clientId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no clientId claim.");

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current advisor's ID from their login token so they can only access their own records.
        /// </summary>
        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the conversation between the current user (either an advisor or a client) and the specified client. The conversation includes all messages exchanged between the two parties.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets a list of conversation summaries for the specified advisor. Each summary includes the client ID, client name, last message, and timestamp of the last message.
        /// </summary>
        /// <param name="advisorId"></param>
        /// <returns></returns>
        [HttpGet("advisor/{advisorId:int}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<ConversationSummaryDto>>> GetForAdvisor(int advisorId)
        {
            if (advisorId != CurrentAdvisorId)
                return Forbid();

            return Ok(await _messageService.GetConversationsForAdvisorAsync(advisorId));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Sends a message from the current user (either an advisor or a client) to the specified recipient.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
