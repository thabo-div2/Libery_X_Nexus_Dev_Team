using API.DTOs.Meetings;
using Shared.Models.Enums;

namespace API.Services.Interfaces
{
    public interface IMeetingService
    {
        Task<MeetingDto?> GetByIdAsync(int meetingId);
        Task<IEnumerable<MeetingDto>> GetForClientAsync(int clientId);
        Task<IEnumerable<MeetingDto>> GetUpcomingAsync(int? clientId, int? advisorId);
        Task<IEnumerable<MeetingDto>> GetByDateRangeAsync(DateTime from, DateTime to, int advisorId);
        Task<IEnumerable<MeetingDto>> GetByStatusAsync(MeetingStatus status, int advisorId);
        Task<MeetingDto> BookAsync(BookMeetingRequest request);
        Task<MeetingDto> RescheduleAsync(int meetingId, RescheduleMeetingRequest request);
        /// <summary>byAdvisor = true when the advisor made the change: the meeting stays confirmed and the advisor isn't notified about their own action.</summary>
        Task<MeetingDto> RescheduleAsync(int meetingId, RescheduleMeetingRequest request, bool byAdvisor);
        Task<MeetingDto> CancelAsync(int meetingId);
        /// <summary>byAdvisor = true when the advisor cancelled (or declined a request): the advisor isn't notified about their own action.</summary>
        Task<MeetingDto> CancelAsync(int meetingId, bool byAdvisor);
        Task<MeetingDto> ConfirmAsync(int meetingId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
