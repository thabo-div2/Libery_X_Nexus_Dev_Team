using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Implementations
{
    public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
    {
        public DocumentRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(dbContextFactory) { }

        public async Task<IEnumerable<Document>> GetByClientIdAsync(int clientId) 
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var docs = await context.Documents.Where(d => d.ClientId == clientId).OrderByDescending(d => d.UpdateAt).AsNoTracking().ToListAsync();

            return docs;
        }

        public async Task<IEnumerable<Document>> GetVisibleToClientAsync(int clientId) 
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var docs = await context.Documents.Where(d => d.ClientId == clientId && d.VisibleToClient).OrderByDescending(d => d.UpdateAt).AsNoTracking().ToListAsync();

            return docs;
        }

        public async Task<IEnumerable<Document>> GetByPolicyIdAsync(int policyId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var docs = await context.Documents.Where(d => d.PolicyId == policyId).OrderByDescending(d => d.UpdateAt).AsNoTracking().ToListAsync();

            return docs;
        }

        public async Task<IEnumerable<Document>> GetByTypeAsync(DocumentType documentType) 
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var docs = await context.Documents.Where(d => d.DocumentType == documentType).OrderByDescending(d => d.UpdateAt).AsNoTracking().ToListAsync();

            return docs;
        }

        public async Task<bool> BelongsToClientAsync(int documentId, int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.Documents.AnyAsync(d => d.DocumentId == documentId && d.ClientId == clientId);
        }
    }
}
