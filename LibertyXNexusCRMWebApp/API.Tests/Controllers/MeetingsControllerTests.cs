using API.Controllers;
using API.DTOs.Clients;
using API.DTOs.Meetings;
using API.Services.Interfaces;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Tests for the MeetingsController, mainly who is allowed to book and change which meeting.
    /// </summary>
    public class MeetingsControllerTests
    {
        private const int MyAdvisorId = 5;
        private const int OtherAdvisorId = 9;

        private readonly Mock<IMeetingService> _meetingService = new();
        private readonly Mock<IClientService> _clientService = new();
        private readonly MeetingsController _sut;

        /// <summary>
        /// Sets up the controller with fake services.
        /// </summary>
        public MeetingsControllerTests()
        {
            _sut = new MeetingsController(_meetingService.Object, _clientService.Object);
        }

        private static MeetingDto MeetingOf(int meetingId, int clientId) => new()
        {
            MeetingId = meetingId,
            ClientId = clientId,
            MeetingDate = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 60,
            Status = "Requested"
        };

        private void ClientBelongsToAdvisor(int clientId, int advisorId)
        {
            _clientService
                .Setup(s => s.GetByIdAsync(clientId))
                .ReturnsAsync(new ClientDetailDto { ClientId = clientId, AdvisorId = advisorId });
        }

        /// <summary>
        /// A client can open their own meeting.
        /// </summary>
        [Fact]
        public async Task GetById_WhenMeetingBelongsToTheClient_ReturnsOk()
        {
            var meeting = MeetingOf(8, 1);
            _meetingService.Setup(s => s.GetByIdAsync(8)).ReturnsAsync(meeting);
            _sut.AsClient(1);

            var result = await _sut.GetById(8);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(meeting, ok.Value);
        }

        /// <summary>
        /// A client asking for another client's meeting gets a 404.
        /// </summary>
        [Fact]
        public async Task GetById_WhenMeetingBelongsToAnotherClient_ReturnsNotFound()
        {
            _meetingService.Setup(s => s.GetByIdAsync(8)).ReturnsAsync(MeetingOf(8, 2));
            _sut.AsClient(1);

            var result = await _sut.GetById(8);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// A meeting that does not exist gives a 404.
        /// </summary>
        [Fact]
        public async Task GetById_WhenMeetingDoesNotExist_ReturnsNotFound()
        {
            _meetingService.Setup(s => s.GetByIdAsync(404)).ReturnsAsync((MeetingDto?)null);
            _sut.AsClient(1);

            var result = await _sut.GetById(404);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// A client cannot list another client's meetings.
        /// </summary>
        [Fact]
        public async Task GetForClient_WhenClientAsksForAnotherClient_ReturnsForbid()
        {
            _sut.AsClient(1);

            var result = await _sut.GetForClient(2);

            Assert.IsType<ForbidResult>(result.Result);
            _meetingService.Verify(s => s.GetForClientAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// A client cannot ask for every upcoming meeting, only an advisor can.
        /// </summary>
        [Fact]
        public async Task GetUpcoming_WhenClientDoesNotNameThemselves_ReturnsForbid()
        {
            _sut.AsClient(1);

            var result = await _sut.GetUpcoming(null);

            Assert.IsType<ForbidResult>(result.Result);
            _meetingService.Verify(s => s.GetUpcomingAsync(It.IsAny<int?>()), Times.Never);
        }

        /// <summary>
        /// An advisor can ask for all upcoming meetings.
        /// </summary>
        [Fact]
        public async Task GetUpcoming_WhenAdvisorDoesNotNameAClient_ReturnsAllUpcoming()
        {
            var meetings = new List<MeetingDto> { MeetingOf(8, 1) };
            _meetingService.Setup(s => s.GetUpcomingAsync(null)).ReturnsAsync(meetings);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.GetUpcoming(null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(meetings, ok.Value);
        }

        /// <summary>
        /// A bad date range gives a 400 with the reason.
        /// </summary>
        [Fact]
        public async Task GetByRange_WhenTheRangeIsInvalid_ReturnsBadRequest()
        {
            _meetingService
                .Setup(s => s.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), MyAdvisorId))
                .ThrowsAsync(new ArgumentException("'to' must be after 'from'."));
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.GetByRange(new DateTime(2026, 11, 10), new DateTime(2026, 11, 1));

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("'to' must be after 'from'.", ControllerTestHelper.MessageOf(badRequest.Value));
        }

        /// <summary>
        /// A client cannot book a meeting on behalf of another client.
        /// </summary>
        [Fact]
        public async Task Book_WhenClientBooksForAnotherClient_ReturnsForbidAndDoesNotBook()
        {
            _sut.AsClient(1);

            var result = await _sut.Book(new BookMeetingRequest { ClientId = 2 });

            Assert.IsType<ForbidResult>(result.Result);
            _meetingService.Verify(s => s.BookAsync(It.IsAny<BookMeetingRequest>()), Times.Never);
        }

        /// <summary>
        /// A client can book a meeting for themselves.
        /// </summary>
        [Fact]
        public async Task Book_WhenClientBooksForThemselves_ReturnsCreated()
        {
            _meetingService.Setup(s => s.BookAsync(It.IsAny<BookMeetingRequest>())).ReturnsAsync(MeetingOf(8, 1));
            _sut.AsClient(1);

            var result = await _sut.Book(new BookMeetingRequest { ClientId = 1 });

            Assert.IsType<CreatedAtActionResult>(result.Result);
        }

        /// <summary>
        /// An advisor cannot book for a client that belongs to another advisor.
        /// </summary>
        [Fact]
        public async Task Book_WhenAdvisorBooksForAnotherAdvisorsClient_ReturnsForbid()
        {
            ClientBelongsToAdvisor(2, OtherAdvisorId);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Book(new BookMeetingRequest { ClientId = 2 });

            Assert.IsType<ForbidResult>(result.Result);
        }

        /// <summary>
        /// A time slot that is already taken gives a 409.
        /// </summary>
        [Fact]
        public async Task Book_WhenTheSlotIsTaken_ReturnsConflict()
        {
            _meetingService
                .Setup(s => s.BookAsync(It.IsAny<BookMeetingRequest>()))
                .ThrowsAsync(new InvalidOperationException("That time slot is already booked."));
            _sut.AsClient(1);

            var result = await _sut.Book(new BookMeetingRequest { ClientId = 1 });

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal("That time slot is already booked.", ControllerTestHelper.MessageOf(conflict.Value));
        }

        /// <summary>
        /// Invalid booking details give a 400 with the reason.
        /// </summary>
        [Fact]
        public async Task Book_WhenDetailsAreInvalid_ReturnsBadRequest()
        {
            _meetingService
                .Setup(s => s.BookAsync(It.IsAny<BookMeetingRequest>()))
                .ThrowsAsync(new ArgumentException("Meetings cannot be booked in the past."));
            _sut.AsClient(1);

            var result = await _sut.Book(new BookMeetingRequest { ClientId = 1 });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Meetings cannot be booked in the past.", ControllerTestHelper.MessageOf(badRequest.Value));
        }

        /// <summary>
        /// A client cannot reschedule another client's meeting.
        /// </summary>
        [Fact]
        public async Task Reschedule_WhenMeetingBelongsToAnotherClient_ReturnsNotFoundAndDoesNotReschedule()
        {
            _meetingService.Setup(s => s.GetByIdAsync(8)).ReturnsAsync(MeetingOf(8, 2));
            _sut.AsClient(1);

            var result = await _sut.Reschedule(8, new RescheduleMeetingRequest { NewMeetingDate = DateTime.UtcNow.AddDays(3) });

            Assert.IsType<NotFoundObjectResult>(result.Result);
            _meetingService.Verify(s => s.RescheduleAsync(It.IsAny<int>(), It.IsAny<RescheduleMeetingRequest>()), Times.Never);
        }

        /// <summary>
        /// A client can cancel their own meeting.
        /// </summary>
        [Fact]
        public async Task Cancel_WhenMeetingBelongsToTheClient_ReturnsOk()
        {
            var cancelled = MeetingOf(8, 1);
            cancelled.Status = "Cancelled";
            _meetingService.Setup(s => s.GetByIdAsync(8)).ReturnsAsync(MeetingOf(8, 1));
            _meetingService.Setup(s => s.CancelAsync(8)).ReturnsAsync(cancelled);
            _sut.AsClient(1);

            var result = await _sut.Cancel(8);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(cancelled, ok.Value);
        }

        /// <summary>
        /// A meeting that cannot be cancelled in its current state gives a 409.
        /// </summary>
        [Fact]
        public async Task Cancel_WhenTheMeetingCannotBeCancelled_ReturnsConflict()
        {
            _meetingService.Setup(s => s.GetByIdAsync(8)).ReturnsAsync(MeetingOf(8, 1));
            _meetingService
                .Setup(s => s.CancelAsync(8))
                .ThrowsAsync(new InvalidOperationException("A completed meeting cannot be cancelled."));
            _sut.AsClient(1);

            var result = await _sut.Cancel(8);

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        /// <summary>
        /// An advisor can confirm a meeting for one of their clients.
        /// </summary>
        [Fact]
        public async Task Confirm_WhenMeetingBelongsToTheAdvisorsClient_ReturnsOk()
        {
            var confirmed = MeetingOf(8, 2);
            confirmed.Status = "Confirmed";
            _meetingService.Setup(s => s.GetByIdAsync(8)).ReturnsAsync(MeetingOf(8, 2));
            _meetingService.Setup(s => s.ConfirmAsync(8)).ReturnsAsync(confirmed);
            ClientBelongsToAdvisor(2, MyAdvisorId);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Confirm(8);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(confirmed, ok.Value);
        }

        /// <summary>
        /// An advisor cannot confirm a meeting for another advisor's client.
        /// </summary>
        [Fact]
        public async Task Confirm_WhenMeetingBelongsToAnotherAdvisorsClient_ReturnsNotFoundAndDoesNotConfirm()
        {
            _meetingService.Setup(s => s.GetByIdAsync(8)).ReturnsAsync(MeetingOf(8, 2));
            ClientBelongsToAdvisor(2, OtherAdvisorId);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Confirm(8);

            Assert.IsType<NotFoundObjectResult>(result.Result);
            _meetingService.Verify(s => s.ConfirmAsync(It.IsAny<int>()), Times.Never);
        }
    }
}