using API.DTOs.Advisor;

namespace API.Services.Interfaces
{
    public interface IAdvisorService
    {
        Task<AdvisorDashboardDto> GetDashboardAsync(int advisorId);
    }
}
