using API.Data;
using API.DTOs.DocumentRequests;
using API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Services.Implementations
{
    /// <summary>
    /// Documents the advisor has asked a client for. The client sees them as a checklist,
    /// and uploading a document of the requested type ticks the request off.
    /// </summary>
    public class DocumentRequestService : IDocumentRequestService
    {
        private const int MaxNoteLength = 300;

        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly INotificationService _notificationService;

        public DocumentRequestService(IDbContextFactory<ApplicationDbContext> contextFactory, INotificationService notificationService)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets a client's requests: outstanding ones first, then the newest.
        /// </summary>
        public async Task<IEnumerable<DocumentRequestDto>> GetForClientAsync(int clientId)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            var requests = await db.DocumentRequests
                .AsNoTracking()
                .Include(r => r.Policy)
                .Where(r => r.ClientId == clientId)
                .OrderBy(r => r.FulfilledAt != null)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(MapToDto).ToList();
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Asks a client for a document and lets them know. A client can only have one outstanding
        /// request per document type and policy (or general scope).
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<DocumentRequestDto> CreateAsync(int clientId, CreateDocumentRequestRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!Enum.IsDefined(request.DocumentType))
            {
                throw new ArgumentException("Choose the type of document to ask for.");
            }

            var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

            if (note is not null && note.Length > MaxNoteLength)
            {
                throw new ArgumentException($"The note cannot be longer than {MaxNoteLength} characters.");
            }

            await using var db = await _contextFactory.CreateDbContextAsync();

            var clientExists = await db.Clients.AnyAsync(c => c.ClientId == clientId);
            if (!clientExists)
            {
                throw new KeyNotFoundException($"Client {clientId} was not found.");
            }

            Policy? policy = null;
            if (request.PolicyId is int policyId)
            {
                policy = await db.Policies.FirstOrDefaultAsync(p => p.PolicyId == policyId && p.ClientId == clientId && !p.IsCatalogueItem);
                if (policy is null) throw new ArgumentException("Choose a policy belonging to this client, or General documents.");
            }

            var alreadyAsked = await db.DocumentRequests
                .AnyAsync(r => r.ClientId == clientId && r.DocumentType == request.DocumentType && r.PolicyId == request.PolicyId && r.FulfilledAt == null);

            if (alreadyAsked)
            {
                throw new InvalidOperationException("There is already an outstanding request for this document in the selected policy or general documents.");
            }

            var entity = new DocumentRequest
            {
                ClientId = clientId,
                PolicyId = request.PolicyId,
                Policy = policy,
                DocumentType = request.DocumentType,
                Note = note,
                CreatedAt = DateTime.UtcNow
            };

            db.DocumentRequests.Add(entity);
            await db.SaveChangesAsync();

            // The request is saved, so a failed notification must not turn this into an error.
            try
            {
                await _notificationService.NotifyClientAsync(
                    clientId,
                    NotificationType.PendingAction,
                    $"Your advisor needs a document from you: {Describe(request.DocumentType)} ({(policy is null ? "General documents" : policy.PolicyName)}).",
                    "/client-documents");
            }
            catch (Exception)
            {
                // Ignore - the request itself was saved.
            }

            return MapToDto(entity);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Removes a request. The advisor check is part of the query, so one advisor can never
        /// remove a request that belongs to another advisor's client.
        /// </summary>
        public async Task<bool> CancelAsync(int documentRequestId, int advisorId)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.DocumentRequests
                .FirstOrDefaultAsync(r => r.DocumentRequestId == documentRequestId && r.Client.AdvisorId == advisorId);

            if (entity is null)
            {
                return false;
            }

            db.DocumentRequests.Remove(entity);
            await db.SaveChangesAsync();
            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Called after a document is uploaded. If the client was asked for that type of document,
        /// the oldest outstanding request for that same policy or general scope is marked as done.
        /// </summary>
        public async Task<bool> FulfilMatchingAsync(int clientId, DocumentType documentType, int documentId)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            var document = await db.Documents.AsNoTracking().FirstOrDefaultAsync(d =>
                d.DocumentId == documentId && d.ClientId == clientId && d.DocumentType == documentType);
            if (document is null) return false;

            var entity = await db.DocumentRequests
                .MatchingOutstanding(document)
                .OrderBy(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (entity is null)
            {
                return false;
            }

            entity.FulfilledAt = DateTime.UtcNow;
            entity.FulfilledDocumentId = documentId;
            await db.SaveChangesAsync();
            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// "ProofOfAddress" becomes "proof of address", for the notification text.
        /// </summary>
        private static string Describe(DocumentType type) => type switch
        {
            DocumentType.CertifiedIdCopy => "certified ID copy",
            DocumentType.ProofOfAddress => "proof of address",
            DocumentType.BankStatement => "bank statement",
            DocumentType.Payslip => "payslip",
            DocumentType.TaxCertificate => "tax certificate",
            DocumentType.PolicyStatement => "policy statement",
            DocumentType.AmendmentForm => "amendment form",
            DocumentType.ServicePackageSummary => "service package summary",
            _ => "a document"
        };

        private static DocumentRequestDto MapToDto(DocumentRequest r) => new()
        {
            DocumentRequestId = r.DocumentRequestId,
            ClientId = r.ClientId,
            PolicyId = r.PolicyId,
            PolicyName = r.Policy?.PolicyName,
            DocumentType = r.DocumentType.ToString(),
            Note = r.Note,
            CreatedAt = r.CreatedAt,
            FulfilledAt = r.FulfilledAt,
            IsFulfilled = r.FulfilledAt.HasValue
        };
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
