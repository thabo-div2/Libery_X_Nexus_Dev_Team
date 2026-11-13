using API.DTOs.Availability;

namespace API.Services.Interfaces
{
    /// <summary>
    /// The advisor's working hours: saving them, and checking them before a client messages or books.
    /// </summary>
    public interface IAvailabilityService
    {
        Task<AdvisorScheduleDto> GetScheduleAsync(int advisorId);
        Task<AdvisorScheduleDto> SaveWeeklyHoursAsync(int advisorId, UpdateWeeklyHoursRequest request);
        Task<AdvisorScheduleDto> SaveOverrideAsync(int advisorId, SaveHoursOverrideRequest request);
        Task<bool> DeleteOverrideAsync(int advisorId, string date);
        Task<AdvisorAvailabilityDto> GetAvailabilityAsync(int advisorId);

        /// <summary>The times on a date (yyyy-MM-dd) when a meeting of this length fits in the advisor's hours and diary.</summary>
        Task<MeetingSlotsDto> GetOpenSlotsAsync(int advisorId, string date, int durationMinutes);

        /// <summary>Throws AdvisorUnavailableException when the advisor is out of hours right now.</summary>
        Task EnsureCanMessageAsync(int advisorId);

        /// <summary>Throws AdvisorUnavailableException when the meeting doesn't fit inside the advisor's hours for that day.</summary>
        Task EnsureMeetingWithinHoursAsync(int advisorId, DateTime meetingDate, int durationMinutes);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
