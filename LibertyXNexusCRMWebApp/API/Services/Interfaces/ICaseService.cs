using API.DTOs.Cases;

namespace API.Services.Interfaces
{
    public interface ICaseService
    {
        Task<IEnumerable<CaseStatusDto>> GetForClientAsync(int clientId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
