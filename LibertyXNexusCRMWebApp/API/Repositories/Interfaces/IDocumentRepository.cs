using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Interfaces
{
    public interface IDocumentRepository : IGenericRepository<Document> 
    {
        Task<IEnumerable<Document>> GetByClientIdAsync(int clientId);
        Task<IEnumerable<Document>> GetVisibleToClientAsync(int clientId);
        Task<IEnumerable<Document>> GetByPolicyIdAsync(int policyId);
        Task<IEnumerable<Document>> GetByTypeAsync(DocumentType documentType);
        Task<bool> BelongsToClientAsync(int documentId, int clientId);
    }
}
