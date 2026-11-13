using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace API.Repositories.Implementations
{
    /// <summary>
    /// Reads and saves an advisor's working hours and custom dates.
    /// </summary>
    public class AdvisorHoursRepository : IAdvisorHoursRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        public AdvisorHoursRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<AdvisorWorkingHours>> GetWeeklyHoursAsync(int advisorId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.AdvisorWorkingHours
                .AsNoTracking()
                .Where(h => h.AdvisorId == advisorId)
                .ToListAsync();
        }

        /// <summary>
        /// Saves the advisor's default week. A day that already has a row is updated, otherwise it is added,
        /// and it all goes in as one save so the week is never left half changed.
        /// </summary>
        public async Task SaveWeeklyHoursAsync(int advisorId, IEnumerable<AdvisorWorkingHours> days)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var existing = await context.AdvisorWorkingHours
                .Where(h => h.AdvisorId == advisorId)
                .ToListAsync();

            foreach (var day in days)
            {
                var row = existing.FirstOrDefault(h => h.DayOfWeek == day.DayOfWeek);

                if (row is null)
                {
                    context.AdvisorWorkingHours.Add(new AdvisorWorkingHours
                    {
                        AdvisorId = advisorId,
                        DayOfWeek = day.DayOfWeek,
                        IsWorkingDay = day.IsWorkingDay,
                        StartTime = day.StartTime,
                        EndTime = day.EndTime
                    });
                }
                else
                {
                    row.IsWorkingDay = day.IsWorkingDay;
                    row.StartTime = day.StartTime;
                    row.EndTime = day.EndTime;
                }
            }

            await context.SaveChangesAsync();
        }

        public async Task<AdvisorHoursOverride?> GetOverrideAsync(int advisorId, DateOnly date)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.AdvisorHoursOverrides
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.AdvisorId == advisorId && o.Date == date);
        }

        public async Task<List<AdvisorHoursOverride>> GetOverridesFromAsync(int advisorId, DateOnly from)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.AdvisorHoursOverrides
                .AsNoTracking()
                .Where(o => o.AdvisorId == advisorId && o.Date >= from)
                .OrderBy(o => o.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Saves custom hours for a date. A date can only have one set, so saving it again replaces the first.
        /// </summary>
        public async Task SaveOverrideAsync(AdvisorHoursOverride hoursOverride)
        {
            ArgumentNullException.ThrowIfNull(hoursOverride);

            using var context = await _dbContextFactory.CreateDbContextAsync();

            var existing = await context.AdvisorHoursOverrides
                .FirstOrDefaultAsync(o => o.AdvisorId == hoursOverride.AdvisorId && o.Date == hoursOverride.Date);

            if (existing is null)
            {
                context.AdvisorHoursOverrides.Add(hoursOverride);
            }
            else
            {
                existing.IsWorkingDay = hoursOverride.IsWorkingDay;
                existing.StartTime = hoursOverride.StartTime;
                existing.EndTime = hoursOverride.EndTime;
                existing.Note = hoursOverride.Note;
            }

            await context.SaveChangesAsync();
        }

        public async Task<bool> DeleteOverrideAsync(int advisorId, DateOnly date)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var existing = await context.AdvisorHoursOverrides
                .FirstOrDefaultAsync(o => o.AdvisorId == advisorId && o.Date == date);

            if (existing is null)
            {
                return false;
            }

            context.AdvisorHoursOverrides.Remove(existing);
            await context.SaveChangesAsync();
            return true;
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
