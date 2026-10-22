using API.Controllers;
using API.DTOs.Invitations;
using API.Services.Interfaces;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Tests for the InvitationController: advisors create invitations, anyone can check a token.
    /// </summary>
    public class InvitationControllerTests
    {
        private const int MyAdvisorId = 5;

        private readonly Mock<IInvitationService> _invitationService = new();
        private readonly InvitationController _sut;

        /// <summary>
        /// Sets up the controller with a fake invitation service.
        /// </summary>
        public InvitationControllerTests()
        {
            _sut = new InvitationController(_invitationService.Object);
        }

        /// <summary>
        /// The invitation is created for the advisor in the token, not for an id supplied by the caller.
        /// </summary>
        [Fact]
        public async Task Create_UsesTheAdvisorIdFromTheToken()
        {
            var response = new InvitationResponse { Success = true, Message = "Invitation sent." };
            _invitationService.Setup(s => s.CreateAsync(MyAdvisorId, "jane@nexus.test")).ReturnsAsync(response);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Create(new CreateInvitationRequest { Email = "jane@nexus.test" });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(response, ok.Value);
            _invitationService.Verify(s => s.CreateAsync(MyAdvisorId, "jane@nexus.test"), Times.Once);
        }

        /// <summary>
        /// A failed invitation gives a 400 that carries the reason.
        /// </summary>
        [Fact]
        public async Task Create_WhenServiceReportsFailure_ReturnsBadRequestWithTheResponse()
        {
            var response = new InvitationResponse { Success = false, Message = "That client is already registered." };
            _invitationService.Setup(s => s.CreateAsync(MyAdvisorId, It.IsAny<string>())).ReturnsAsync(response);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Create(new CreateInvitationRequest { Email = "jane@nexus.test" });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Same(response, badRequest.Value);
        }

        /// <summary>
        /// Checking a token always answers 200, and the body says whether the token is valid.
        /// </summary>
        [Fact]
        public async Task Validate_WhenTokenIsInvalid_StillReturnsOkWithValidFalse()
        {
            var response = new InvitationValidationResponse { Valid = false, Message = "This invitation has expired." };
            _invitationService.Setup(s => s.ValidateAsync("bad-token")).ReturnsAsync(response);

            var result = await _sut.Validate("bad-token");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<InvitationValidationResponse>(ok.Value);
            Assert.False(body.Valid);
        }

        /// <summary>
        /// A good token returns the invited email and advisor.
        /// </summary>
        [Fact]
        public async Task Validate_WhenTokenIsValid_ReturnsTheInvitationDetails()
        {
            var response = new InvitationValidationResponse { Valid = true, Email = "jane@nexus.test", AdvisorId = MyAdvisorId, AdvisorName = "Ratul" };
            _invitationService.Setup(s => s.ValidateAsync("good-token")).ReturnsAsync(response);

            var result = await _sut.Validate("good-token");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<InvitationValidationResponse>(ok.Value);
            Assert.True(body.Valid);
            Assert.Equal("jane@nexus.test", body.Email);
        }
    }
}