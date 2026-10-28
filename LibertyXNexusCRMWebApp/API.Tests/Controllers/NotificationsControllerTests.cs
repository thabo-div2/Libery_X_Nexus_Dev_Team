using API.Controllers;
using API.Services.Interfaces;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Tests for marking a single notification as read.
    /// </summary>
    public class NotificationsControllerTests
    {
        private const int MyAdvisorId = 5;

        private readonly Mock<INotificationService> _notificationService = new();
        private readonly NotificationsController _sut;

        /// <summary>
        /// Sets up the controller with a fake notification service, signed in as advisor 5.
        /// </summary>
        public NotificationsControllerTests()
        {
            _sut = new NotificationsController(_notificationService.Object).AsAdvisor(MyAdvisorId);
        }

        /// <summary>
        /// When the notification is marked as read, the API answers 204 No Content.
        /// </summary>
        [Fact]
        public async Task MarkAsRead_WhenNotificationIsUpdated_ReturnsNoContent()
        {
            _notificationService.Setup(s => s.MarkAsReadForAdvisorAsync(10, MyAdvisorId)).ReturnsAsync(true);

            var result = await _sut.MarkAsRead(10);

            Assert.IsType<NoContentResult>(result);
        }

        /// <summary>
        /// When the notification can't be found, the API answers 404 with a JSON message
        /// instead of an empty response.
        /// </summary>
        [Fact]
        public async Task MarkAsRead_WhenNotificationIsNotFound_ReturnsNotFoundWithMessage()
        {
            _notificationService.Setup(s => s.MarkAsReadForAdvisorAsync(99, MyAdvisorId)).ReturnsAsync(false);

            var result = await _sut.MarkAsRead(99);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Notification not found.", ControllerTestHelper.MessageOf(notFound.Value));
        }

        /// <summary>
        /// The notification is always looked up for the advisor in the token,
        /// so an advisor can't mark someone else's notification as read.
        /// </summary>
        [Fact]
        public async Task MarkAsRead_UsesTheAdvisorIdFromTheToken()
        {
            _notificationService.Setup(s => s.MarkAsReadForAdvisorAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);

            await _sut.MarkAsRead(10);

            _notificationService.Verify(s => s.MarkAsReadForAdvisorAsync(10, MyAdvisorId), Times.Once);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
