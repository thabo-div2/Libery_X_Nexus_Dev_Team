using API.DTOs.Documents;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Services.Implementations
{
    /// <summary>
    /// Service for managing documents, including uploading, retrieving, and deleting documents associated with clients and policies.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
         "application/pdf","image/png","image/jpeg","application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

        private readonly IDocumentRepository _documentRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IBlobStorageService _blobStorageService;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentService"/> class.
        /// </summary>
        /// <param name="documentRepository"></param>
        /// <param name="clientRepository"></param>
        /// <param name="policyRepository"></param>
        /// <param name="blobStorageService"></param>
        public DocumentService(IDocumentRepository documentRepository,IClientRepository clientRepository,IPolicyRepository policyRepository,IBlobStorageService blobStorageService)
        {
            _documentRepository = documentRepository;
            _clientRepository = clientRepository;
            _policyRepository = policyRepository;
            _blobStorageService = blobStorageService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves a document by its ID and maps it to a DocumentDto.
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        public async Task<DocumentDto?> GetByIdAsync(int documentId) 
        {
            var document = await _documentRepository.GetByIdAsync(documentId) ?? new Document();

            return new DocumentDto
            {
                DocumentId = document.DocumentId,
                ClientId = document.ClientId,
                PolicyId = document.PolicyId,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSizeBytes = document.FileSizeBytes,
                DocumentType = document.DocumentType.ToString(),
                VisibleToClient = document.VisibleToClient,
                UploadedBy = document.UploadedBy,
                UpdateAt = document.UpdateAt
            };
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all documents associated with a specific client and maps them to DocumentDto objects.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<DocumentDto>> GetForClientAsync(int clientId)
        {
            var documents = await _documentRepository.GetByClientIdAsync(clientId);
            return documents.Select(MapToDto);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all documents that are visible to a specific client and maps them to DocumentDto objects.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<DocumentDto>> GetVisibleToClientAsync(int clientId)
        {
            var documents = await _documentRepository.GetVisibleToClientAsync(clientId);
            return documents.Select(MapToDto);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all documents associated with a specific policy and maps them to DocumentDto objects.
        /// </summary>
        /// <param name="policyId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<DocumentDto>> GetForPolicyAsync(int policyId)
        {
            var documents = await _documentRepository.GetByPolicyIdAsync(policyId);
            return documents.Select(MapToDto);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all documents of a specific type and maps them to DocumentDto objects.
        /// </summary>
        /// <param name="documentType"></param>
        /// <returns></returns>
        public async Task<IEnumerable<DocumentDto>> GetByTypeAsync(DocumentType documentType)
        {
            var documents = await _documentRepository.GetByTypeAsync(documentType);
            return documents.Select(MapToDto);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Uploads a document, validates the request, and stores it in blob storage. Returns a DocumentDto representing the uploaded document.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<DocumentDto> UploadAsync(UploadDocumentRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                throw new ArgumentException("A non-empty file is required");
            }

            if (request.File.Length > MaxFileSizeBytes)
            {
                throw new ArgumentException($"File exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");
            }

            if (!AllowedContentTypes.Contains(request.File.ContentType))
            {
                throw new ArgumentException($"File type '{request.File.ContentType}' is not permitted.");
            }

            var clientExists = await _clientRepository.ExistsAsync(request.ClientId);
            if (!clientExists)
            {
                throw new KeyNotFoundException($"Client {request.ClientId} was not found.");
            }

            var policyExists = await _policyRepository.ExistsAsync(request.PolicyId);
            if (!policyExists)
            {
                throw new KeyNotFoundException($"Policy {request.PolicyId} was not found.");
            }

            //ensuring that the policy is one that the clients actually holds, not someone elses
            var policyBelongsToClient = await _policyRepository.BelongsToClientAsync(request.PolicyId, request.ClientId);
            if (!policyBelongsToClient)
            {
                throw new InvalidOperationException($"Policy {request.PolicyId} does not belong to client {request.ClientId}.");
            }

            await using var stream = request.File.OpenReadStream();
            var blobReference = await _blobStorageService.UploadAsync(stream, request.File.FileName, request.File.ContentType);

            var document = new Document
            {
                ClientId = request.ClientId,
                PolicyId = request.PolicyId,
                FileName = request.File.FileName,
                BlobReference = blobReference,
                ContentType = request.File.ContentType,
                FileSizeBytes = request.File.Length,
                DocumentType = request.DocumentType,
                VisibleToClient = request.VisibleToClient,
                UploadedBy = request.UploadedBy,
                UpdateAt = DateTime.UtcNow
            };

            var created = await _documentRepository.AddAsync(document);
            return MapToDto(created);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves a download URI for a specific document, allowing access to the document stored in blob storage.
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task<Uri> GetDownloadUriAsync(int documentId)
        {
            var document = await _documentRepository.GetByIdAsync(documentId) ?? throw new KeyNotFoundException($"Document {documentId} was not found");
            return await _blobStorageService.GetReadSasUriAsync(document.BlobReference);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Sets the visibility of a specific document for clients and updates the document's metadata accordingly.
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="visibleToClient"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task<DocumentDto> SetVisibilityAsync(int documentId, bool visibleToClient)
        {
            var document = await _documentRepository.GetByIdAsync(documentId) ?? throw new KeyNotFoundException($"Document {documentId} was not found.");

            document.VisibleToClient = visibleToClient;
            document.UpdateAt = DateTime.UtcNow;

            await _documentRepository.UpdateAsync(document);
            return MapToDto(document);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes a specific document, removing it from both the repository and blob storage. Returns true if the deletion was successful, or false if the document was not found.
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(int documentId)
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document is null)
            {
                return false;
            }

            await _blobStorageService.DeleteAsync(document.BlobReference);
            await _documentRepository.DeleteAsync(documentId);
            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Maps a Document entity to a DocumentDto, transforming the data for external use.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static DocumentDto MapToDto(Document document) => new()
        {
            DocumentId = document.DocumentId,
            ClientId = document.ClientId,
            PolicyId = document.PolicyId,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSizeBytes = document.FileSizeBytes,
            DocumentType = document.DocumentType.ToString(),
            VisibleToClient = document.VisibleToClient,
            UploadedBy = document.UploadedBy,
            UpdateAt = document.UpdateAt
        };
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
