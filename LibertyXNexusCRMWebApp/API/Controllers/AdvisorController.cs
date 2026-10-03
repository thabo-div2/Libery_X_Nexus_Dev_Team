using API.DTOs.Advisor;
using API.Identity;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing advisor-related operations. Provides endpoints for advisors to access their dashboard and other advisor-specific functionalities.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Advisor)]
    public class AdvisorController : ControllerBase
    {
        private readonly IAdvisorService _advisorService;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="AdvisorController"/> class with the specified advisor service.
        /// </summary>
        /// <param name="advisorService"></param>
        public AdvisorController(IAdvisorService advisorService)
        {
            _advisorService = advisorService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves the dashboard data for the authenticated advisor. The dashboard includes various metrics and information relevant to the advisor's activities.
        /// </summary>
        /// <returns></returns>
        [HttpGet("dashboard")]
        public async Task<ActionResult<AdvisorDashboardDto>> GetDashboard()
        {
            if (!int.TryParse(User.FindFirstValue("advisorId"), out var advisorId))
            {
                return Unauthorized(new { message = "The authenticated advisor is not linked to an advisor record." });
            }

            var dashboard = await _advisorService.GetDashboardAsync(advisorId);
            return Ok(dashboard);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
