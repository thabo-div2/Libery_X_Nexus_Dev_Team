using API.DTOs.Documents;
using Shared.Models.Enums;

namespace API.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentDto?> GetByIdAsync(int documentId);
        Task<IEnumerable<DocumentDto>> GetForClientAsync(int clientId);
        Task<IEnumerable<DocumentDto>> GetVisibleToClientAsync(int clientId);
        Task<IEnumerable<DocumentDto>> GetForPolicyAsync(int policyId);
        Task<IEnumerable<DocumentDto>> GetByTypeAsync(DocumentType documentType);
        Task<DocumentDto> UploadAsync(UploadDocumentRequest request);
        Task<Uri> GetDownloadUriAsync(int documentId);
        Task<DocumentDto> SetVisibilityAsync(int documentId, bool visibleToClient);
        Task<bool> DeleteAsync(int documentId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
