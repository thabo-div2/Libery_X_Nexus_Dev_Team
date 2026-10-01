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

        private int CurrentAdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id)
            ? id
            : throw new InvalidOperationException("Token has no advisorId claim.");

        [HttpGet("dashboard")]
        public async Task<ActionResult<AdvisorDashboardDto>> GetDashboard()
        {
            var advisorId = CurrentAdvisorId;

            var dashboard = await _advisorService.GetDashboardAsync(advisorId);
            return Ok(dashboard);
        }
    }
}
