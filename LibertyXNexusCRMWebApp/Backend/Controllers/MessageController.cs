using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly MessageService _messageService;

        public MessageController(MessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetConversation(int clientId)
        {
            var messages = await _messageService.GetConversationAsync(clientId);
            return Ok(messages);
        }

        [HttpGet("advisor/{advisorId}")]
        public async Task<IActionResult> GetConversations(int advisorId)
        {
            var conversations = await _messageService.GetConversationsForAdvisorAsync(advisorId);
            return Ok(conversations);
        }

        [HttpPost]
        public async Task<IActionResult> Send(SendMessageRequest request)
        {
            var message = await _messageService.SendAsync(request);
            return Ok(message);
        }
    }
}
