using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Implementations
{
    public class MeetingRepository : GenericRepository<Meeting>, IMeetingRepository
    {
        public MeetingRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(dbContextFactory) { }

        public async Task<IEnumerable<Meeting>> GetClientIdAsync(int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var meetings = await context.Meetings
                                    .Where(m => m.ClientId == clientId)
                                    .OrderByDescending(m => m.MeetingDate)
                                    .AsNoTracking()
                                    .ToListAsync();

            return meetings;
        }

        public async Task<IEnumerable<Meeting>> GetUpcomingMeetingAsync(int? clientId = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var query = context.Meetings
                        .Include(m => m.Client)
                        .Where(m => m.MeetingDate >= DateTime.UtcNow &&
                                m.Status != MeetingStatus.Cancelled);

            if (clientId.HasValue)
                query = query.Where(m => m.ClientId == clientId.Value);

            return await query.OrderBy(m => m.MeetingDate).AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Meeting>> GetByDateRangeAsync(DateTime from, DateTime to)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var meetings = await context.Meetings
                                    .Include(m => m.Client)
                                    .Where(m => m.MeetingDate >= from && m.MeetingDate <= to)
                                    .OrderBy(m => m.MeetingDate)
                                    .AsNoTracking()
                                    .ToListAsync();

            return meetings;
        }

        public async Task<IEnumerable<Meeting>> GetByStatusAsync(MeetingStatus status)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var meetings = await context.Meetings
                                    .Include(m => m.Client)
                                    .Where(m => m.Status == status)
                                    .OrderBy(m => m.MeetingDate)
                                    .AsNoTracking()
                                    .ToListAsync();

            return meetings;
        }

        public async Task<bool> HasConflictAsync(DateTime start, int durationMinutes, int? excludeMeetingId = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var end = start.AddMinutes(durationMinutes);

            var query = context.Meetings
                    .Where(m => m.Status != MeetingStatus.Cancelled);

            if (excludeMeetingId.HasValue)
                query = query.Where(m => m.MeetingId != excludeMeetingId.Value);

            return await query.AnyAsync(m =>
                start < m.MeetingDate.AddMinutes(m.DurationMinutes) &&
                m.MeetingDate < end);
        }

        public async Task<bool> BelongsToClientAsync(int meetingId, int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.Meetings.AnyAsync(m => m.MeetingId == meetingId && m.ClientId == clientId);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
