using API.DTOs.Meetings;
using API.Identity;
using API.Services.Implementations;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Enums;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MeetingsController : ControllerBase
    {
        private readonly IMeetingService meetingService_;
        private readonly IClientService _clientService;

        public MeetingsController(IMeetingService meetingService, IClientService clientService)
        {
            meetingService_ = meetingService;
            _clientService = clientService;
        }

        private bool IsAdvisor => User.IsInRole(AppRoles.Advisor);

        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        private int CurrentClientId => int.TryParse(User.FindFirstValue("clientId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no clientId claim.");

        private async Task<bool> CanAccessClientAsync(int clientId)
        {
            if (!IsAdvisor)
            {
                return CurrentClientId == clientId;
            }

            var client = await _clientService.GetByIdAsync(clientId);
            return client is not null && client.AdvisorId == CurrentAdvisorId;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MeetingDto>> GetById(int id)
        {
            var meeting = await meetingService_.GetByIdAsync(id);
            if (meeting is null || !await CanAccessClientAsync(meeting.ClientId))
            {
                return NotFound(new { message = $"Meeting {id} was not found." });
            }
            return Ok(meeting);
        }

        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<MeetingDto>>> GetForClient(int clientId)
        {
            if (!await CanAccessClientAsync(clientId)) return Forbid();
            return Ok(await meetingService_.GetForClientAsync(clientId));
        }

        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<MeetingDto>>> GetUpcoming([FromQuery] int? clientId)
        {
            if (clientId.HasValue)
            {
                if (!await CanAccessClientAsync(clientId.Value)) return Forbid();
                return Ok(await meetingService_.GetUpcomingAsync(clientId));
            }

            // No clientId supplied = "all upcoming meetings", which only
            // makes sense as the adviser's own dashboard view. A client
            // calling with no clientId would otherwise see every client's
            // upcoming meetings system-wide.
            if (!IsAdvisor) return Forbid();
            return Ok(await meetingService_.GetUpcomingAsync(null));
        }

        /// <summary>Advisor-only: a firm-wide date range with no per-client scoping.</summary>
        [HttpGet("range")]
        [Authorize(Roles = AppRoles.Advisor)]
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

        /// <summary>Advisor-only - same reasoning as GetByRange.</summary>
        [HttpGet("status/{status}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<MeetingDto>>> GetByStatus(MeetingStatus status)
        {
            return Ok(await meetingService_.GetByStatusAsync(status));
        }

        [HttpPost]
        public async Task<ActionResult<MeetingDto>> Book([FromBody] BookMeetingRequest request)
        {
            // A client can only book for themselves; the adviser can book
            // for any of their own clients. Without this check, a client
            // could set ClientId to someone else's id in the request body.
            if (!await CanAccessClientAsync(request.ClientId)) return Forbid();

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
            var existing = await meetingService_.GetByIdAsync(id);
            if (existing is null || !await CanAccessClientAsync(existing.ClientId))
            {
                return NotFound(new { message = $"Meeting {id} was not found." });
            }

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
            var existing = await meetingService_.GetByIdAsync(id);
            if (existing is null || !await CanAccessClientAsync(existing.ClientId))
            {
                return NotFound(new { message = $"Meeting {id} was not found." });
            }

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

        /// <summary>
        /// Advisor-only - confirming a meeting request is an adviser action
        /// in this business flow, not something a client does to their own
        /// request.
        /// </summary>
        [HttpPut("{id:int}/confirm")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<MeetingDto>> Confirm(int id)
        {
            var existing = await meetingService_.GetByIdAsync(id);
            if (existing is null || !await CanAccessClientAsync(existing.ClientId))
            {
                return NotFound(new { message = $"Meeting {id} was not found." });
            }

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
