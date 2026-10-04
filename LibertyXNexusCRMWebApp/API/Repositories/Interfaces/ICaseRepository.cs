using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Interfaces
{
    public interface ICaseRepository : IGenericRepository<Case>
    {
        Task<Case?> GetByPolicyIdAsync(int policyId);
        Task<IEnumerable<Case>> GetByStatusAsync(CaseStatus status);
        Task<IEnumerable<Case>> GetByClientIdAsync(int clientId);
        Task UpdateStatusAsync(int caseId, CaseStatus status, string? notes = null);
        Task<Case> MarkStepCompleteAsync(int caseId, CaseStep step);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
