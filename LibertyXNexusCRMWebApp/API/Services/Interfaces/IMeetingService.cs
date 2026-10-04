using API.DTOs.Meetings;
using Shared.Models.Enums;

namespace API.Services.Interfaces
{
    public interface IMeetingService
    {
        Task<MeetingDto?> GetByIdAsync(int meetingId);
        Task<IEnumerable<MeetingDto>> GetForClientAsync(int clientId);
        Task<IEnumerable<MeetingDto>> GetUpcomingAsync(int? clientId);
        Task<IEnumerable<MeetingDto>> GetByDateRangeAsync(DateTime from, DateTime to, int advisorId);
        Task<IEnumerable<MeetingDto>> GetByStatusAsync(MeetingStatus status, int advisorId);
        Task<MeetingDto> BookAsync(BookMeetingRequest request);
        Task<MeetingDto> RescheduleAsync(int meetingId, RescheduleMeetingRequest request);
        Task<MeetingDto> CancelAsync(int meetingId);
        Task<MeetingDto> ConfirmAsync(int meetingId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
