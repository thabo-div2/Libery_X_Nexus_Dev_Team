using API.DTOs.Availability;
using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// The advisor's working hours. The advisor manages their own default week and custom dates,
    /// and a client can see whether their advisor is available right now.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AvailabilityController : ControllerBase
    {
        private readonly IAvailabilityService _availabilityService;
        private readonly IClientService _clientService;

        public AvailabilityController(IAvailabilityService availabilityService, IClientService clientService)
        {
            _availabilityService = availabilityService;
            _clientService = clientService;
        }

        /// <summary>
        /// The signed-in advisor's ID from their login token, so they can only ever change their own hours.
        /// </summary>
        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        private int CurrentClientId => int.TryParse(User.FindFirstValue("clientId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no clientId claim.");

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the advisor's default week and upcoming custom dates.
        /// </summary>
        [HttpGet("advisor/hours")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<AdvisorScheduleDto>> GetSchedule()
        {
            return Ok(await _availabilityService.GetScheduleAsync(CurrentAdvisorId));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Saves the advisor's default week.
        /// </summary>
        [HttpPut("advisor/hours")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<AdvisorScheduleDto>> SaveWeeklyHours([FromBody] UpdateWeeklyHoursRequest request)
        {
            try
            {
                return Ok(await _availabilityService.SaveWeeklyHoursAsync(CurrentAdvisorId, request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Adds or replaces the custom hours for one date.
        /// </summary>
        [HttpPut("advisor/overrides")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<ActionResult<AdvisorScheduleDto>> SaveOverride([FromBody] SaveHoursOverrideRequest request)
        {
            try
            {
                return Ok(await _availabilityService.SaveOverrideAsync(CurrentAdvisorId, request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Removes the custom hours for one date (yyyy-MM-dd), so the default hours apply again.
        /// </summary>
        [HttpDelete("advisor/overrides/{date}")]
        [Authorize(Roles = AppRoles.Advisor)]
        public async Task<IActionResult> DeleteOverride(string date)
        {
            try
            {
                var deleted = await _availabilityService.DeleteOverrideAsync(CurrentAdvisorId, date);
                return deleted ? NoContent() : NotFound(new { message = "There are no custom hours for that date." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Tells a client whether their own advisor is available right now, and when they are next back if not.
        /// </summary>
        [HttpGet("status")]
        [Authorize(Roles = AppRoles.Client)]
        public async Task<ActionResult<AdvisorAvailabilityDto>> GetStatusForClient()
        {
            var client = await _clientService.GetByIdAsync(CurrentClientId);

            if (client is null)
            {
                return NotFound(new { message = "Your profile could not be found." });
            }

            // No advisor yet, so there are no hours to be outside of.
            if (client.AdvisorId is not int advisorId)
            {
                return Ok(new AdvisorAvailabilityDto());
            }

            return Ok(await _availabilityService.GetAvailabilityAsync(advisorId));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Lists the open meeting times with the client's own advisor on a date (yyyy-MM-dd).
        /// </summary>
        [HttpGet("slots")]
        [Authorize(Roles = AppRoles.Client)]
        public async Task<ActionResult<MeetingSlotsDto>> GetSlots([FromQuery] string? date, [FromQuery] int durationMinutes = 60)
        {
            var client = await _clientService.GetByIdAsync(CurrentClientId);

            if (client is null)
            {
                return NotFound(new { message = "Your profile could not be found." });
            }

            // No advisor yet, so there is no diary to read.
            if (client.AdvisorId is not int advisorId)
            {
                return Ok(new MeetingSlotsDto());
            }

            try
            {
                return Ok(await _availabilityService.GetOpenSlotsAsync(advisorId, date ?? string.Empty, durationMinutes));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
