using API.DTOs.Cases;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Shared.Models;
using Shared.Models.Enums;

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

        public async Task<CaseStatusDto> MarkStepCompleteAsync(int caseId, CaseStep step)
        {
            var updated = await caseRepository_.MarkStepCompleteAsync(caseId, step);
            return MapToDto(updated);
        }

        private static CaseStatusDto MapToDto(Case c) => new()
        {
            CaseId = c.CaseId,
            Status = c.Status.ToString(),
            PolicyId = c.PolicyId,
            PolicyName = c.Policy?.PolicyName,
            Notes = c.Notes,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            DetailsSubmittedAt = c.DetailsSubmittedAt,
            AdviserReviewAt = c.AdviserReviewAt,
            FicaVerifiedAt = c.FicaVerifiedAt,
            SubmittedToLibertyAt = c.SubmittedToLibertyAt,
            PolicyIssuedAt = c.PolicyIssuedAt,
        };
    }
}
