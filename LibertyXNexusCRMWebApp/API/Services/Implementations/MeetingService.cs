using API.DTOs.Meetings;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Services.Implementations
{
    public class MeetingService : IMeetingService
    {
        private readonly IMeetingRepository meetingRepository_;
        private readonly IClientRepository clientRepository_;

        public MeetingService(IMeetingRepository meetingRepository, IClientRepository clientRepository)
        {
            meetingRepository_ = meetingRepository;
            clientRepository_ = clientRepository;
        }

        public async Task<MeetingDto?> GetByIdAsync(int meetingId)
        {
            var meeting = await meetingRepository_.GetByIdAsync(meetingId);
            return meeting is null ? null : await MapToDtoAsync(meeting);
        }

        public async Task<IEnumerable<MeetingDto>> GetForClientAsync(int clientId)
        {
            var meetings = await meetingRepository_.GetClientIdAsync(clientId);
            return await MapManyAsync(meetings);
        }

        public async Task<IEnumerable<MeetingDto>> GetUpcomingAsync(int? clientId)
        {
            var meetings = await meetingRepository_.GetUpcomingMeetingAsync(clientId);
            return await MapManyAsync(meetings);
        }

        public async Task<IEnumerable<MeetingDto>> GetByDateRangeAsync(DateTime from, DateTime to)
        {
            if (from > to)
            {
                throw new ArgumentException("The start of the date range must be before the end");
            }

            var meetings = await meetingRepository_.GetByDateRangeAsync(from, to);
            return await MapManyAsync(meetings);
        }

        public async Task<IEnumerable<MeetingDto>> GetByStatusAsync(MeetingStatus status)
        {
            var meetings = await meetingRepository_.GetByStatusAsync(status);
            return await MapManyAsync(meetings);
        }

        public async Task<MeetingDto> BookAsync(BookMeetingRequest request) 
        {
            ValidateFutureDate(request.MeetingDate);

            var clientExists = await clientRepository_.ExistsAsync(request.ClientId);
           
            if (!clientExists) {
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
            return await MapToDtoAsync(created);
        }

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
            return await MapToDtoAsync(meeting);
        }

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

        private static void ValidateFutureDate(DateTime date)
        {
            if (date <= DateTime.UtcNow)
            {
                throw new ArgumentException("Meeting date must be in the future.");
            }
        }

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
