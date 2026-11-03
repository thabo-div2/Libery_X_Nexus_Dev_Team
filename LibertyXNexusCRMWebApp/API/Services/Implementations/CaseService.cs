using API.DTOs.Cases;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Services.Implementations
{
    /// <summary>
    /// Handles a client's cases.
    /// </summary>
    public class CaseService : ICaseService
    {
        private readonly ICaseRepository caseRepository_;
        private readonly INotificationService notificationService_;

        /// <summary>
        /// Sets up the service.
        /// </summary>
        public CaseService(ICaseRepository caseRepository, INotificationService notificationService)
        {
            caseRepository_ = caseRepository;
            notificationService_ = notificationService;
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
        /// Ticks off a step on the case and tells the client their application moved forward.
        /// </summary>
        public async Task<CaseStatusDto> MarkStepCompleteAsync(int caseId, CaseStep step)
        {
            var updated = await caseRepository_.MarkStepCompleteAsync(caseId, step);

            if (await caseRepository_.GetClientIdForCaseAsync(caseId) is int clientId)
            {
                await notificationService_.NotifyClientAsync(
                    clientId,
                    NotificationType.CaseUpdated,
                    $"Your application has moved forward: {StepName(step)} is done.",
                    "/client");
            }

            return MapToDto(updated);
        }

        private static string StepName(CaseStep step) => step switch
        {
            CaseStep.DetailsSubmitted => "Details submitted",
            CaseStep.AdviserReview => "Adviser review",
            CaseStep.FicaVerification => "FICA verification",
            CaseStep.SubmittedToLiberty => "Submitted to Liberty",
            CaseStep.PolicyIssued => "Policy issued",
            _ => step.ToString()
        };

        public Task<int?> GetClientIdForCaseAsync(int caseId)
        {
            return caseRepository_.GetClientIdForCaseAsync(caseId);
        }

        /// <summary>
        /// Turns a case into a DTO.
        /// </summary>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
