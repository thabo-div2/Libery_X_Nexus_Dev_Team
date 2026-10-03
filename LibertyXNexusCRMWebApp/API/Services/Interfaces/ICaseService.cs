using API.DTOs.Cases;
using Shared.Models.Enums;

namespace API.Services.Interfaces
{
    public interface ICaseService
    {
        Task<IEnumerable<CaseStatusDto>> GetForClientAsync(int clientId);
        Task<CaseStatusDto> MarkStepCompleteAsync(int caseId, CaseStep step);
    }
}
