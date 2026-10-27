using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Interfaces 
{

    public interface IPolicyRepository : IGenericRepository<Policy>
    {
        Task<IEnumerable<Policy>> GetByClientIdAsync(int clientId);
        Task<IEnumerable<Policy>> GetCatalogoueAsync();
        Task<IEnumerable<Policy>> GetByStatusAsync(PolicyStatus status, int advisorId);
        Task<Policy?> GetWithDocumentAsync(int policyId);
        Task<bool> BelongsToClientAsync(int policyId, int clientId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
