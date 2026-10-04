using API.DTOs.Clients;
using API.DTOs.Meetings;
using API.Repositories.Interfaces;
using API.Services.Implementations;
using API.Services.Interfaces;
using Moq;
using Shared.Models;
using Shared.Models.Enums;
using Xunit;

namespace API.Tests.Services
{
    /// <summary>
    /// Tests for the MeetingService.
    /// </summary>
    public class MeetingServiceTests
    {
        private readonly Mock<IMeetingRepository> _meetingRepository = new();
        private readonly Mock<IClientRepository> _clientRepository = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly MeetingService _sut;

        /// <summary>
        /// Sets up the service with fakes.
        /// </summary>
        public MeetingServiceTests()
        {
            _sut = new MeetingService(_meetingRepository.Object, _clientRepository.Object, _notificationService.Object);
        }

        /// <summary>
        /// Makes a fake client.
        /// </summary>
        private static Client MakeClient(int id = 1) => new()
        {
            ClientId = id,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@nexus.test"
        };

        /// <summary>
        /// Can't book in the past.
        /// </summary>
        [Fact]
        public async Task BookAsync_WithPastDate_ThrowsArgumentException()
        {
            var request = new BookMeetingRequest
            {
                ClientId = 1,
                MeetingDate = DateTime.UtcNow.AddDays(-1),
                DurationMinutes = 60
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.BookAsync(request));
        }

        /// <summary>
        /// Can't book for an unknown client.
        /// </summary>
        [Fact]
        public async Task BookAsync_WithUnknownClient_ThrowsKeyNotFoundException()
        {
            _clientRepository.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

            var request = new BookMeetingRequest
            {
                ClientId = 99,
                MeetingDate = DateTime.UtcNow.AddDays(1),
                DurationMinutes = 60
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.BookAsync(request));
        }

        /// <summary>
        /// Can't book a taken time slot.
        /// </summary>
        [Fact]
        public async Task BookAsync_WithConflictingSlot_ThrowsInvalidOperationException()
        {
            _clientRepository.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _clientRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeClient(1));

            _meetingRepository
                .Setup(r => r.HasConflictAsync(It.IsAny<DateTime>(), 60, null))
                .ReturnsAsync(true);

            var request = new BookMeetingRequest
            {
                ClientId = 1,
                MeetingDate = DateTime.UtcNow.AddDays(1),
                DurationMinutes = 60
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.BookAsync(request));
        }

        /// <summary>
        /// Valid booking creates the meeting.
        /// </summary>
        [Fact]
        public async Task BookAsync_WithValidRequest_CreatesAndReturnsMeeting()
        {
            _clientRepository.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _meetingRepository
                .Setup(r => r.HasConflictAsync(It.IsAny<DateTime>(), 60, null))
                .ReturnsAsync(false);

            var meetingDate = DateTime.UtcNow.AddDays(1);
            Meeting? captured = null;

            _meetingRepository
                .Setup(r => r.AddAsync(It.IsAny<Meeting>()))
                .Callback<Meeting>(m => { m.MeetingId = 55; captured = m; })
                .ReturnsAsync(() => captured!);

            _clientRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeClient(1));

            var request = new BookMeetingRequest
            {
                ClientId = 1,
                MeetingDate = meetingDate,
                DurationMinutes = 60,
                Notes = "Initial consultation"
            };

            var result = await _sut.BookAsync(request);

            Assert.Equal(55, result.MeetingId);
            Assert.Equal("Requested", result.Status);
            Assert.Equal("Jane Doe", result.ClientName);
        }

        /// <summary>
        /// Can't cancel a completed meeting.
        /// </summary>
        [Fact]
        public async Task CancelAsync_WhenMeetingIsCompleted_ThrowsInvalidOperationException()
        {
            var meeting = new Meeting { MeetingId = 1, ClientId = 1, Status = MeetingStatus.Completed, MeetingDate = DateTime.UtcNow };
            _meetingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(meeting);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CancelAsync(1));
        }

        /// <summary>
        /// Can cancel a requested meeting.
        /// </summary>
        [Fact]
        public async Task CancelAsync_WhenMeetingIsRequested_CancelsSuccessfully()
        {
            var meeting = new Meeting { MeetingId = 1, ClientId = 1, Status = MeetingStatus.Requested, MeetingDate = DateTime.UtcNow };
            _meetingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(meeting);
            _meetingRepository.Setup(r => r.UpdateAsync(meeting)).Returns(Task.CompletedTask);
            _clientRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeClient(1));

            var result = await _sut.CancelAsync(1);

            Assert.Equal("Cancelled", result.Status);
        }

        /// <summary>
        /// Can only confirm a requested meeting.
        /// </summary>
        [Fact]
        public async Task ConfirmAsync_WhenMeetingIsNotRequested_ThrowsInvalidOperationException()
        {
            var meeting = new Meeting { MeetingId = 1, ClientId = 1, Status = MeetingStatus.Confirmed, MeetingDate = DateTime.UtcNow };
            _meetingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(meeting);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ConfirmAsync(1));
        }

        /// <summary>
        /// Can confirm a requested meeting.
        /// </summary>
        [Fact]
        public async Task ConfirmAsync_WhenMeetingIsRequested_ConfirmsSuccessfully()
        {
            var meeting = new Meeting { MeetingId = 1, ClientId = 1, Status = MeetingStatus.Requested, MeetingDate = DateTime.UtcNow };
            _meetingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(meeting);
            _meetingRepository.Setup(r => r.UpdateAsync(meeting)).Returns(Task.CompletedTask);
            _clientRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeClient(1));

            var result = await _sut.ConfirmAsync(1);

            Assert.Equal("Confirmed", result.Status);
        }

        /// <summary>
        /// Can't reschedule a cancelled meeting.
        /// </summary>
        [Fact]
        public async Task RescheduleAsync_WhenMeetingIsCancelled_ThrowsInvalidOperationException()
        {
            var meeting = new Meeting { MeetingId = 1, ClientId = 1, Status = MeetingStatus.Cancelled, MeetingDate = DateTime.UtcNow };
            _meetingRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(meeting);

            var request = new RescheduleMeetingRequest { NewMeetingDate = DateTime.UtcNow.AddDays(2) };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RescheduleAsync(1, request));
        }

        /// <summary>
        /// Can't reschedule a missing meeting.
        /// </summary>
        [Fact]
        public async Task RescheduleAsync_ThatDoesNotExist_ThrowsKeyNotFoundException()
        {
            _meetingRepository.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Meeting?)null);

            var request = new RescheduleMeetingRequest { NewMeetingDate = DateTime.UtcNow.AddDays(2) };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.RescheduleAsync(404, request));
        }

        /// <summary>
        /// Start date can't be after end date.
        /// </summary>
        [Fact]
        public async Task GetByDateRangeAsync_WithStartAfterEnd_ThrowsArgumentException()
        {
            var from = DateTime.UtcNow;
            var to = DateTime.UtcNow.AddDays(-1);

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetByDateRangeAsync(from, to, 1));
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
