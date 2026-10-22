using API.Controllers;
using API.DTOs.Advisor;
using API.Services.Interfaces;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Tests for the AdvisorController dashboard endpoint.
    /// </summary>
    public class AdvisorControllerTests
    {
        private readonly Mock<IAdvisorService> _advisorService = new();
        private readonly AdvisorController _sut;

        /// <summary>
        /// Sets up the controller with a fake advisor service.
        /// </summary>
        public AdvisorControllerTests()
        {
            _sut = new AdvisorController(_advisorService.Object);
        }

        /// <summary>
        /// The dashboard is built for the advisor in the token.
        /// </summary>
        [Fact]
        public async Task GetDashboard_ReturnsTheDashboardForTheAdvisorInTheToken()
        {
            var dashboard = new AdvisorDashboardDto { AdvisorFirstName = "Ratul", TotalClients = 12 };
            _advisorService.Setup(s => s.GetDashboardAsync(5)).ReturnsAsync(dashboard);
            _sut.AsAdvisor(5);

            var result = await _sut.GetDashboard();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(dashboard, ok.Value);
            _advisorService.Verify(s => s.GetDashboardAsync(5), Times.Once);
        }

        /// <summary>
        /// A token with no advisor id gives a 401 and no dashboard is built.
        /// </summary>
        [Fact]
        public async Task GetDashboard_WhenTokenHasNoAdvisorId_ReturnsUnauthorized()
        {
            _sut.AsRoleWithoutIds("Advisor");

            var result = await _sut.GetDashboard();

            Assert.IsType<UnauthorizedObjectResult>(result.Result);
            _advisorService.Verify(s => s.GetDashboardAsync(It.IsAny<int>()), Times.Never);
        }
    }
}