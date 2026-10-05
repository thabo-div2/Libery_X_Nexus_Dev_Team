using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Interfaces
{
    public interface IMeetingRepository : IGenericRepository<Meeting>
    {
        Task<IEnumerable<Meeting>> GetClientIdAsync(int clientId);
        Task<IEnumerable<Meeting>> GetUpcomingMeetingAsync(int? clientId = null);
        Task<IEnumerable<Meeting>> GetByDateRangeAsync(DateTime from, DateTime to, int advisorId);
        Task<IEnumerable<Meeting>> GetByStatusAsync(MeetingStatus status, int advisorId);
        Task<bool> HasConflictAsync(DateTime start, int durationMinutes, int advisorId, int? excludeMeetingId = null);
        Task<bool> BelongsToClientAsync(int meetingId, int clientId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
