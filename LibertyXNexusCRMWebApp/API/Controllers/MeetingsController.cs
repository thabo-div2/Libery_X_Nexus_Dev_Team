using API.DTOs.Meetings;
using API.Services.Implementations;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeetingsController : ControllerBase
    {
        private readonly IMeetingService meetingService_;

        public MeetingsController(IMeetingService meetingService)
        {
            meetingService_ = meetingService;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MeetingDto>> GetById(int id)
        {
            var meeting = await meetingService_.GetByIdAsync(id);
            return meeting is null ? NotFound(new { message = $"Meeting {id} was not found." }): Ok(meeting);
        }

        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<MeetingDto>>> GetForClient(int clientId)
        {
            return Ok(await meetingService_.GetForClientAsync(clientId));
        }

        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<MeetingDto>>> GetUpcoming([FromQuery] int? clientId)
        {
            return Ok(await meetingService_.GetUpcomingAsync(clientId));
        }

        [HttpGet("range")]
        public async Task<ActionResult<IEnumerable<MeetingDto>>> GetByRange([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            try
            {
                return Ok(await meetingService_.GetByDateRangeAsync(from, to));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<MeetingDto>>> GetByStatus(MeetingStatus status)
        {
            return Ok(await meetingService_.GetByStatusAsync(status));
        }

        [HttpPost]
        public async Task<ActionResult<MeetingDto>> Book([FromBody] BookMeetingRequest request)
        {
            try
            {
                var created = await meetingService_.BookAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.MeetingId }, created);
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

        [HttpPut("{id:int}/reschedule")]
        public async Task<ActionResult<MeetingDto>> Reschedule(int id, [FromBody] RescheduleMeetingRequest request)
        {
            try
            {
                return Ok(await meetingService_.RescheduleAsync(id, request));
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

        [HttpPut("{id:int}/cancel")]
        public async Task<ActionResult<MeetingDto>> Cancel(int id)
        {
            try
            {
                return Ok(await meetingService_.CancelAsync(id));
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

        [HttpPut("{id:int}/confirm")]
        public async Task<ActionResult<MeetingDto>> Confirm(int id)
        {
            try
            {
                return Ok(await meetingService_.ConfirmAsync(id));
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
