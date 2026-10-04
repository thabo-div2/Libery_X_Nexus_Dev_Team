using API.DTOs.Advisor;

namespace API.Services.Interfaces
{
    public interface IAdvisorService
    {
        Task<AdvisorDashboardDto> GetDashboardAsync(int advisorId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
