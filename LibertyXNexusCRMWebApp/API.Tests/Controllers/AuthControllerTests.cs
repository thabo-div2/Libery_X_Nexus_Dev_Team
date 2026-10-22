using API.Controllers;
using API.DTOs.Auth;
using API.Services.Interfaces;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Tests for the AuthController: login, register, password reset and the token echo.
    /// </summary>
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authService = new();
        private readonly AuthController _sut;

        /// <summary>
        /// Sets up the controller with a fake auth service.
        /// </summary>
        public AuthControllerTests()
        {
            _sut = new AuthController(_authService.Object);
        }

        /// <summary>
        /// Wrong email or password gives a 401 with an error message.
        /// </summary>
        [Fact]
        public async Task Login_WhenCredentialsAreInvalid_ReturnsUnauthorizedWithMessage()
        {
            _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>())).ReturnsAsync((AuthResponse?)null);

            var result = await _sut.Login(new LoginRequest { Email = "jane@nexus.test", Password = "wrong" });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Invalid email or password", ControllerTestHelper.MessageOf(unauthorized.Value));
        }

        /// <summary>
        /// Good credentials give a 200 with the token response.
        /// </summary>
        [Fact]
        public async Task Login_WhenCredentialsAreValid_ReturnsOkWithToken()
        {
            var response = new AuthResponse { AccessToken = "token", Email = "jane@nexus.test", Role = "Client" };
            _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>())).ReturnsAsync(response);

            var result = await _sut.Login(new LoginRequest { Email = "jane@nexus.test", Password = "Correct-Password-1" });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(response, ok.Value);
        }

        /// <summary>
        /// A failed registration gives a 400 with the reason.
        /// </summary>
        [Fact]
        public async Task Register_WhenServiceReportsFailure_ReturnsBadRequestWithError()
        {
            _authService
                .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>()))
                .ReturnsAsync(new RegisterResult { Success = false, Error = "Invitation is no longer valid." });

            var result = await _sut.Register(new RegisterRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invitation is no longer valid.", ControllerTestHelper.MessageOf(badRequest.Value));
        }

        /// <summary>
        /// A good registration gives a 200 with the token response.
        /// </summary>
        [Fact]
        public async Task Register_WhenServiceSucceeds_ReturnsOkWithResponse()
        {
            var response = new AuthResponse { AccessToken = "token", Email = "jane@nexus.test", Role = "Client" };
            _authService
                .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>()))
                .ReturnsAsync(new RegisterResult { Success = true, Response = response });

            var result = await _sut.Register(new RegisterRequest());

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(response, ok.Value);
        }

        /// <summary>
        /// Forgot password always answers 200, so nobody can use it to find out which emails exist.
        /// </summary>
        [Fact]
        public async Task ForgotPassword_AlwaysReturnsOkWithServiceMessage()
        {
            var response = new ForgotPasswordResponse { Message = "If the account exists, a reset link was sent." };
            _authService.Setup(s => s.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>())).ReturnsAsync(response);

            var result = await _sut.ForgotPassword(new ForgotPasswordRequest { Email = "nobody@nexus.test" });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(response, ok.Value);
        }

        /// <summary>
        /// A bad or expired reset token gives a 400 with the reason.
        /// </summary>
        [Fact]
        public async Task ResetPassword_WhenServiceReportsFailure_ReturnsBadRequestWithError()
        {
            _authService
                .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()))
                .ReturnsAsync(new ResetPasswordResult { Success = false, Error = "Invalid or expired reset token." });

            var result = await _sut.ResetPassword(new ResetPasswordRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid or expired reset token.", ControllerTestHelper.MessageOf(badRequest.Value));
        }

        /// <summary>
        /// A successful reset gives a plain 200.
        /// </summary>
        [Fact]
        public async Task ResetPassword_WhenServiceSucceeds_ReturnsOk()
        {
            _authService
                .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()))
                .ReturnsAsync(new ResetPasswordResult { Success = true });

            var result = await _sut.ResetPassword(new ResetPasswordRequest());

            Assert.IsType<OkResult>(result);
        }

        /// <summary>
        /// The me endpoint echoes the claims from the signed-in user's token.
        /// </summary>
        [Fact]
        public void Me_ReturnsTheClaimsFromTheToken()
        {
            _sut.AsAdvisor(7);

            var result = _sut.Me();

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = ok.Value!;
            Assert.Equal("7", body.GetType().GetProperty("advisorId")!.GetValue(body));
            Assert.Equal("user@nexus.test", body.GetType().GetProperty("email")!.GetValue(body));
        }
    }
}
//-------------------------------------------End of File-------------------------------------------------------