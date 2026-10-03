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
    /// <summary>
    /// Controller for managing meetings. Provides endpoints for advisors and clients to book, reschedule, cancel, confirm, and retrieve meetings.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MeetingsController : ControllerBase
    {
        private readonly IMeetingService meetingService_;
        private readonly IClientService _clientService;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="MeetingsController"/> class with the specified meeting service and client service.
        /// </summary>
        /// <param name="meetingService"></param>
        /// <param name="clientService"></param>
        public MeetingsController(IMeetingService meetingService, IClientService clientService)
        {
            meetingService_ = meetingService;
            _clientService = clientService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets a value indicating whether the current user is an advisor based on their role claim.
        /// </summary>
        private bool IsAdvisor => User.IsInRole(AppRoles.Advisor);

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current advisor's ID from their login token so they can only access their own records.
        /// </summary>
        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current client's ID from their login token so they can only access their own records.
        /// </summary>
        private int CurrentClientId => int.TryParse(User.FindFirstValue("clientId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no clientId claim.");

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Checks if the current user can access the specified client's records. Advisors can access their own clients, while clients can only access their own records.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private async Task<bool> CanAccessClientAsync(int clientId)
        {
            if (!IsAdvisor)
            {
                return CurrentClientId == clientId;
            }

            var client = await _clientService.GetByIdAsync(clientId);
            return client is not null && client.AdvisorId == CurrentAdvisorId;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets a meeting by its ID. Returns 404 if the meeting does not exist or if the current user does not have access to it.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all meetings for a specific client. Returns 403 if the current user does not have access to the specified client's records.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<IEnumerable<MeetingDto>>> GetForClient(int clientId)
        {
            if (!await CanAccessClientAsync(clientId)) return Forbid();
            return Ok(await meetingService_.GetForClientAsync(clientId));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all upcoming meetings for a specific client or for all clients if no clientId is provided. Returns 403 if the current user does not have access to the specified client's records or if a client tries to access all upcoming meetings.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<MeetingDto>>> GetUpcoming([FromQuery] int? clientId)
        {
            if (clientId.HasValue)
            {
                if (!await CanAccessClientAsync(clientId.Value)) return Forbid();
                return Ok(await meetingService_.GetUpcomingAsync(clientId));
            }

            // If no clientId is provided, only an advisor can access all upcoming meetings.
            if (!IsAdvisor) return Forbid();
            return Ok(await meetingService_.GetUpcomingAsync(null));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all meetings within a specified date range. This endpoint is restricted to advisors only, as clients should not be able to access meetings outside of their own records.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all meetings with a specific status. This endpoint is restricted to advisors only, as clients should not be able to access meetings outside of their own records.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet("status/{status}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<IEnumerable<MeetingDto>>> GetByStatus(MeetingStatus status)
        {
            return Ok(await meetingService_.GetByStatusAsync(status));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Books a new meeting. 
        /// Clients can only book meetings for themselves, while advisors can book meetings for any of their own clients. 
        /// Returns 403 if the current user does not have access to the specified client's records.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Reschedules an existing meeting. 
        /// Clients can only reschedule their own meetings, while advisors can reschedule meetings for any of their own clients. 
        /// Returns 404 if the meeting does not exist or if the current user does not have access to it.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Cancels an existing meeting.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Confirms an existing meeting. This endpoint is restricted to advisors only, as clients should not be able to confirm meetings.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
