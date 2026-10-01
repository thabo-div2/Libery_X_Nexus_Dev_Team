using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Interfaces 
{

    public interface IPolicyRepository : IGenericRepository<Policy>
    {
        Task<IEnumerable<Policy>> GetByClientIdAsync(int clientId);
        Task<IEnumerable<Policy>> GetCatalogoueAsync();
        Task<IEnumerable<Policy>> GetByStatusAsync(PolicyStatus status);
        Task<Policy?> GetWithDocumentAsync(int policyId);
        Task<bool> BelongsToClientAsync(int policyId, int clientId);
    }
}
