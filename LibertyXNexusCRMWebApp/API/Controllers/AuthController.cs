using API.DTOs.Auth;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (result is null)
            {
                return Unauthorized(new { messsage = "Invalid email or password" });

            }
            return Ok(result);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Error });
            }
            return Ok(result.Response);
        }

        // Shows what API sees in token
        [HttpGet("me")]
        public ActionResult Me()
        {
            return Ok(new
            {
                userId = User.FindFirstValue("sub"),
                email = User.FindFirstValue("email"),
                advisorId = User.FindFirstValue("advisorId"),
                clientId = User.FindFirstValue("clientId"),
                firstName = User.FindFirstValue("firstName"),
                lastName = User.FindFirstValue("lastName"),
                roles = User.FindAll("role").Select(c => c.Value)
            });
        }
    }
}
