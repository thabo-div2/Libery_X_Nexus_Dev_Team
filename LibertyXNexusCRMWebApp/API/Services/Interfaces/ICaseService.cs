using API.DTOs.Cases;

namespace API.Services.Interfaces
{
    public interface ICaseService
    {
        Task<IEnumerable<CaseStatusDto>> GetForClientAsync(int clientId);
    }
}
