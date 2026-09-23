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

        // Shows what API sees in token
        [HttpGet("me")]
        public ActionResult Me()
        {
            return Ok(new
            {
                userId = User.FindFirstValue("sub"),
                email = User.FindFirstValue("email"),
                roles = User.FindAll("role").Select(c => c.Value)
            });
        }
    }
}
