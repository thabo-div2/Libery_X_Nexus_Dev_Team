using API.DTOs.Cases;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Shared.Models;

namespace API.Services.Implementations
{
    public class CaseService : ICaseService
    {
        private readonly ICaseRepository caseRepository_;

        public CaseService(ICaseRepository caseRepository)
        {
            caseRepository_ = caseRepository;
        }

        public async Task<IEnumerable<CaseStatusDto>> GetForClientAsync(int clientId)
        {
            var cases = await caseRepository_.GetByClientIdAsync(clientId);
            return cases.Select(MapToDto);
        }

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
