using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MeetingController : ControllerBase
    {
        private readonly MeetingService _meetingService;

        public MeetingController(MeetingService meetingService)
        {
            _meetingService = meetingService;
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetForClient(int clientId)
        {
            var meetings = await _meetingService.GetForClientAsync(clientId);
            return Ok(meetings);
        }

        [HttpGet("advisor/{advisorId}")]
        public async Task<IActionResult> GetForAdvisor(int advisorId)
        {
            var meetings = await _meetingService.GetForAdvisorAsync(advisorId);
            return Ok(meetings);
        }

        [HttpPost("request")]
        public async Task<IActionResult> Request(MeetingRequestDto request)
        {
            var meeting = await _meetingService.CreateRequestAsync(request);
            return Ok(meeting);
        }

        [HttpPost("{meetingId}/respond")]
        public async Task<IActionResult> Respond(int meetingId, MeetingResponseDto request)
        {
            var (meeting, error) = await _meetingService.RespondAsync(meetingId, request.Accept);
            if (error is not null)
            {
                return NotFound(new { Success = false, Message = error });
            }

            return Ok(meeting);
        }
    }
}
