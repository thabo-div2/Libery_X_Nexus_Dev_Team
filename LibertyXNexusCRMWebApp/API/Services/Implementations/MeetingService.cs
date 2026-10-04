using API.DTOs.Meetings;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Services.Implementations
{
    /// <summary>
    /// Service for managing meetings between clients and advisors, including booking, rescheduling, confirming, and cancelling meetings.
    /// </summary>
    public class MeetingService : IMeetingService
    {
        private readonly IMeetingRepository meetingRepository_;
        private readonly IClientRepository clientRepository_;
        private readonly INotificationService notificationService_;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="MeetingService"/> class with the specified repositories and notification service.
        /// </summary>
        /// <param name="meetingRepository"></param>
        /// <param name="clientRepository"></param>
        /// <param name="notificationService"></param>
        public MeetingService(IMeetingRepository meetingRepository, IClientRepository clientRepository, INotificationService notificationService)
        {
            meetingRepository_ = meetingRepository;
            clientRepository_ = clientRepository;
            notificationService_ = notificationService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves a meeting by its unique identifier and maps it to a MeetingDto.
        /// </summary>
        /// <param name="meetingId"></param>
        /// <returns></returns>
        public async Task<MeetingDto?> GetByIdAsync(int meetingId)
        {
            var meeting = await meetingRepository_.GetByIdAsync(meetingId);
            return meeting is null ? null : await MapToDtoAsync(meeting);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all meetings associated with a specific client and maps them to a collection of MeetingDto objects.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MeetingDto>> GetForClientAsync(int clientId)
        {
            var meetings = await meetingRepository_.GetClientIdAsync(clientId);
            return await MapManyAsync(meetings);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all upcoming meetings, optionally filtered by a specific client, and maps them to a collection of MeetingDto objects.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MeetingDto>> GetUpcomingAsync(int? clientId)
        {
            var meetings = await meetingRepository_.GetUpcomingMeetingAsync(clientId);
            return await MapManyAsync(meetings);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves meetings that fall within a specified date range and maps them to a collection of MeetingDto objects.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<IEnumerable<MeetingDto>> GetByDateRangeAsync(DateTime from, DateTime to, int advisorId)
        {
            if (from > to)
            {
                throw new ArgumentException("The start of the date range must be before the end");
            }

            var meetings = await meetingRepository_.GetByDateRangeAsync(from, to, advisorId);
            return await MapManyAsync(meetings);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves meetings based on their status (e.g., Requested, Confirmed, Completed, Cancelled) and maps them to a collection of MeetingDto objects.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MeetingDto>> GetByStatusAsync(MeetingStatus status, int advisorId)
        {
            var meetings = await meetingRepository_.GetByStatusAsync(status, advisorId);
            return await MapManyAsync(meetings);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Books a new meeting for a client, ensuring that the meeting date is in the future and that there are no scheduling conflicts. If successful, it notifies the advisor of the new meeting request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<MeetingDto> BookAsync(BookMeetingRequest request) 
        {
            ValidateFutureDate(request.MeetingDate);

            var client = await clientRepository_.GetByIdAsync(request.ClientId);
            
             if (client is null) {
                 throw new KeyNotFoundException($"Client {request.ClientId} was not found");
             }

            var hasConflict = await meetingRepository_.HasConflictAsync(request.MeetingDate, request.DurationMinutes);
            if (hasConflict) {
                throw new InvalidOperationException("The advisor already has a meeting in that time slot");
            }

            var meeting = new Meeting
            {
                ClientId = request.ClientId,
                MeetingDate = request.MeetingDate,
                DurationMinutes = request.DurationMinutes,
                MeetingType = request.MeetingType,
                Location = request.Location,
                Notes = request.Notes,
                Status = MeetingStatus.Requested,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var created = await meetingRepository_.AddAsync(meeting);

            if (client.AdvisorId is int advisorId)
            {
                await notificationService_.NotifyAdvisorAsync(
                    advisorId,
                    NotificationType.MeetingBooked,
                    $"{client.FullName} requested a meeting for {meeting.MeetingDate:ddd d MMM, HH:mm}.",
                    "/calendar",
                    client.ClientId);
            }

            return await MapToDtoAsync(created);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Reschedules an existing meeting to a new date and time, ensuring that the new date is in the future and that there are no scheduling conflicts.
        /// If successful, it notifies the advisor of the rescheduled meeting.
        /// </summary>
        /// <param name="meetingId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<MeetingDto> RescheduleAsync(int meetingId, RescheduleMeetingRequest request) 
        {
          var meeting = await meetingRepository_.GetByIdAsync(meetingId) ?? throw new KeyNotFoundException($"Meeting {meetingId} does not exist");

            if (meeting.Status is MeetingStatus.Cancelled or MeetingStatus.Completed) 
            {
                throw new InvalidOperationException($"A meeting with status '{meeting.Status}' cannot be rescheduled");
            }

            ValidateFutureDate(request.NewMeetingDate);

            var newDuration = request.DurationMinutes ?? meeting.DurationMinutes;

            var hasConflict = await meetingRepository_.HasConflictAsync(request.NewMeetingDate, newDuration, excludeMeetingId: meetingId);

            if (hasConflict)
            {
                throw new InvalidOperationException("The advisor already has a meeting in that time slot");
            }
            meeting.MeetingDate = request.NewMeetingDate;
            meeting.DurationMinutes = newDuration;
            meeting.Status = MeetingStatus.Requested;
            meeting.UpdatedAt = DateTime.UtcNow;

            await meetingRepository_.UpdateAsync(meeting);

            var client = await clientRepository_.GetByIdAsync(meeting.ClientId);

            if (client?.AdvisorId is int advisorId)
            {
                await notificationService_.NotifyAdvisorAsync(
                    advisorId,
                    NotificationType.MeetingCancelled,
                    $"{client.FullName} cancelled a meeting for {meeting.MeetingDate:ddd d MMM, HH:mm}.",
                    "/calendar",
                    client.ClientId);
            }

            return await MapToDtoAsync(meeting);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Cancels an existing meeting, ensuring that completed meetings cannot be cancelled. If successful, it notifies the advisor of the cancelled meeting.
        /// </summary>
        /// <param name="meetingId"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<MeetingDto> CancelAsync(int meetingId)
        {
            var meeting = await meetingRepository_.GetByIdAsync(meetingId) ?? throw new KeyNotFoundException($"Meeting {meetingId} was not found.");

            if (meeting.Status == MeetingStatus.Completed)
            {
                throw new InvalidOperationException("A completed meeting cannot be cancelled.");
            }
            meeting.Status = MeetingStatus.Cancelled;
            meeting.UpdatedAt = DateTime.UtcNow;

            await meetingRepository_.UpdateAsync(meeting);
            return await MapToDtoAsync(meeting);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Confirms a meeting that is currently in the 'Requested' status, changing its status to 'Confirmed'. If successful, it notifies the advisor of the confirmed meeting.
        /// </summary>
        /// <param name="meetingId"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<MeetingDto> ConfirmAsync(int meetingId)
        {
            var meeting = await meetingRepository_.GetByIdAsync(meetingId) ?? throw new KeyNotFoundException($"Meeting {meetingId} was not found.");

            if (meeting.Status != MeetingStatus.Requested)
            {
                throw new InvalidOperationException($"Only meetings with status 'Requested' can be confirmed (current status: '{meeting.Status}').");
            }

            meeting.Status = MeetingStatus.Confirmed;
            meeting.UpdatedAt = DateTime.UtcNow;

            await meetingRepository_.UpdateAsync(meeting);
            return await MapToDtoAsync(meeting);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Validates that the provided meeting date is in the future. If the date is in the past or present, it throws an ArgumentException.
        /// </summary>
        /// <param name="date"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void ValidateFutureDate(DateTime date)
        {
            if (date <= DateTime.UtcNow)
            {
                throw new ArgumentException("Meeting date must be in the future.");
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Maps a Meeting entity to a MeetingDto, including fetching the associated client's full name for display purposes.
        /// </summary>
        /// <param name="meeting"></param>
        /// <returns></returns>
        private async Task<MeetingDto> MapToDtoAsync(Meeting meeting)
        {

            var client = await clientRepository_.GetByIdAsync(meeting.ClientId);

            return new MeetingDto
            {
                MeetingId = meeting.MeetingId,
                ClientId = meeting.ClientId,
                ClientName = client?.FullName,
                MeetingDate = meeting.MeetingDate,
                DurationMinutes = meeting.DurationMinutes,
                MeetingType = meeting.MeetingType,
                Location = meeting.Location,
                Status = meeting.Status.ToString(),
                Notes = meeting.Notes,
                CreatedAt = meeting.CreatedAt,
                UpdatedAt = meeting.UpdatedAt
            };
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Maps a collection of Meeting entities to a collection of MeetingDto objects by iterating through each meeting and mapping it individually.
        /// </summary>
        /// <param name="meetings"></param>
        /// <returns></returns>
        private async Task<IEnumerable<MeetingDto>> MapManyAsync(IEnumerable<Meeting> meetings)
        {
            var dtos = new List<MeetingDto>();
            foreach (var meeting in meetings)
            {
                dtos.Add(await MapToDtoAsync(meeting));
            }
            return dtos;
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
