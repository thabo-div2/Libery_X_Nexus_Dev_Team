using Shared.Models;

namespace API.Repositories.Interfaces
{
    /// <summary>
    /// Reads and saves an advisor's working hours and custom dates.
    /// </summary>
    public interface IAdvisorHoursRepository
    {
        Task<List<AdvisorWorkingHours>> GetWeeklyHoursAsync(int advisorId);
        Task SaveWeeklyHoursAsync(int advisorId, IEnumerable<AdvisorWorkingHours> days);
        Task<AdvisorHoursOverride?> GetOverrideAsync(int advisorId, DateOnly date);
        Task<List<AdvisorHoursOverride>> GetOverridesFromAsync(int advisorId, DateOnly from);
        Task SaveOverrideAsync(AdvisorHoursOverride hoursOverride);
        Task<bool> DeleteOverrideAsync(int advisorId, DateOnly date);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
