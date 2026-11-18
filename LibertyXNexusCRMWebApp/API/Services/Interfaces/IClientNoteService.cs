using API.DTOs.Notes;

namespace API.Services.Interfaces
{
    /// <summary>
    /// The advisor's private notes about their clients. Every method checks that the client
    /// belongs to the advisor, and throws KeyNotFoundException if not.
    /// </summary>
    public interface IClientNoteService
    {
        Task<IEnumerable<ClientNoteDto>> GetForClientAsync(int clientId, int advisorId);
        Task<ClientNoteDto> CreateAsync(int clientId, int advisorId, CreateClientNoteRequest request);
        Task<bool> DeleteAsync(int clientNoteId, int advisorId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
