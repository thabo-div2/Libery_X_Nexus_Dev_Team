using API.DTOs.Cases;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Shared.Models;

namespace API.Services.Implementations
{
    /// <summary>
    /// Handles a client's cases.
    /// </summary>
    public class CaseService : ICaseService
    {
        private readonly ICaseRepository caseRepository_;

        /// <summary>
        /// Sets up the service.
        /// </summary>
        public CaseService(ICaseRepository caseRepository)
        {
            caseRepository_ = caseRepository;
        }

        /// <summary>
        /// Gets all of a client's cases.
        /// </summary>
        public async Task<IEnumerable<CaseStatusDto>> GetForClientAsync(int clientId)
        {
            var cases = await caseRepository_.GetByClientIdAsync(clientId);
            return cases.Select(MapToDto);
        }

        /// <summary>
        /// Turns a case into a DTO.
        /// </summary>
        private static CaseStatusDto MapToDto(Case c) => new()
        {
            CaseId = c.CaseId,
            PolicyId = c.PolicyId,
            PolicyName = c.Policy?.PolicyName,
            Status = c.Status.ToString(),
            Notes = c.Notes,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
        };
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
