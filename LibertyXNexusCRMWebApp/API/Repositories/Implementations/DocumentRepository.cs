using API.Data;
using API.Services.Implementations;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Implementations
{
    public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
    {
        public DocumentRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(dbContextFactory) { }

        public override async Task<Document> AddAsync(Document entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            using var strategyContext = await _dbContextFactory.CreateDbContextAsync();
            return await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                await using var transaction = await context.Database.BeginTransactionAsync();
                // A retry must insert a fresh identity if a previous transaction was rolled back.
                entity.DocumentId = 0;
                context.Documents.Add(entity);
                await context.SaveChangesAsync();
                var request = await context.DocumentRequests
                    .MatchingOutstanding(entity)
                    .OrderBy(r => r.CreatedAt).FirstOrDefaultAsync();
                if (request is not null)
                {
                    request.FulfilledAt = DateTime.UtcNow;
                    request.FulfilledDocumentId = entity.DocumentId;
                    await context.SaveChangesAsync();
                }
                await transaction.CommitAsync();
                return entity;
            });
        }

        public override async Task DeleteAsync(int id)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var document = await context.Documents.FindAsync(id)
                ?? throw new KeyNotFoundException($"Document {id} was not found.");
            var requests = await context.DocumentRequests
                .Where(r => r.ClientId == document.ClientId && r.FulfilledDocumentId == id).ToListAsync();
            foreach (var request in requests)
            {
                request.FulfilledAt = null;
                request.FulfilledDocumentId = null;
            }
            context.Documents.Remove(document);
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Document>> GetByClientIdAsync(int clientId) 
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var docs = await context.Documents.Include(d => d.Policy).Where(d => d.ClientId == clientId).OrderByDescending(d => d.UpdateAt).AsNoTracking().ToListAsync();

            return docs;
        }

        public async Task<IEnumerable<Document>> GetByAdvisorIdAsync(int advisorId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            // Load the client and policy too, so the advisor's page can group and label documents
            // without making a second call for the names.
            var docs = await context.Documents
                .Include(d => d.Client)
                .Include(d => d.Policy)
                .Where(d => d.Client.AdvisorId == advisorId)
                .OrderByDescending(d => d.UpdateAt)
                .AsNoTracking()
                .ToListAsync();

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

        public async Task<IEnumerable<Document>> GetByTypeAsync(DocumentType documentType, int advisorId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.Documents
                .Where(d => d.DocumentType == documentType && d.Client.AdvisorId == advisorId)
                .OrderByDescending(d => d.UpdateAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> BelongsToClientAsync(int documentId, int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.Documents.AnyAsync(d => d.DocumentId == documentId && d.ClientId == clientId);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
