using API.DTOs.Advisor;
using API.Identity;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Advisor)]
    public class AdvisorController : ControllerBase
    {
        private readonly IAdvisorService _advisorService;

        public AdvisorController(IAdvisorService advisorService)
        {
            _advisorService = advisorService;
        }

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
