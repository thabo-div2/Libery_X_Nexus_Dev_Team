using API.DTOs.Auth;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for handling authentication-related operations such as login, registration, password reset, and retrieving user information from the token.
    /// </summary>
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class with the specified authentication service.
        /// </summary>
        /// <param name="authService"></param>
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Handles user login requests. Validates the provided credentials and returns an authentication response containing an access token if successful.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Handles user registration requests. Validates the provided registration details and returns an authentication response containing an access token if successful.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Handles forgot password requests. Validates the provided email and returns a response indicating whether the user exists and, if so, provides a reset token.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            return Ok(result);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Handles password reset requests. Validates the provided reset token and new password, and returns a response indicating whether the password reset was successful.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok();
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Shows what API sees in the authentication token.
        /// </summary>
        /// <returns></returns>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
