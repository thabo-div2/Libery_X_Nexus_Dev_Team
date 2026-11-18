using API.DTOs.DocumentRequests;
using Shared.Models.Enums;

namespace API.Services.Interfaces
{
    /// <summary>
    /// Documents the advisor has asked a client for.
    /// </summary>
    public interface IDocumentRequestService
    {
        Task<IEnumerable<DocumentRequestDto>> GetForClientAsync(int clientId);
        Task<DocumentRequestDto> CreateAsync(int clientId, CreateDocumentRequestRequest request);

        /// <summary>Removes a request, but only if it belongs to one of this advisor's clients. False if it wasn't found.</summary>
        Task<bool> CancelAsync(int documentRequestId, int advisorId);

        /// <summary>Marks the client's outstanding request for this type of document as done. False if there wasn't one.</summary>
        Task<bool> FulfilMatchingAsync(int clientId, DocumentType documentType, int documentId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
