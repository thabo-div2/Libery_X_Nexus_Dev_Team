using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Interfaces
{
    public interface IDocumentRepository : IGenericRepository<Document> 
    {
        Task<IEnumerable<Document>> GetByClientIdAsync(int clientId);
        Task<IEnumerable<Document>> GetByAdvisorIdAsync(int advisorId);
        Task<IEnumerable<Document>> GetVisibleToClientAsync(int clientId);
        Task<IEnumerable<Document>> GetByPolicyIdAsync(int policyId);
        Task<IEnumerable<Document>> GetByTypeAsync(DocumentType documentType);
        Task<bool> BelongsToClientAsync(int documentId, int clientId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
