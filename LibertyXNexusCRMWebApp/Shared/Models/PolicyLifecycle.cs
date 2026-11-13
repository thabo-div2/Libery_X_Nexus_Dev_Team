using Shared.Models.Enums;

namespace Shared.Models;

/// <summary>Policy state rules, separate from application progress.</summary>
public static class PolicyLifecycle
{
    public static void ValidateTransition(Policy policy, PolicyStatus next, DateTime today)
    {
        if (!Enum.IsDefined(next)) throw new ArgumentException("Choose a valid policy status.");
        if (policy.IsCatalogueItem) throw new InvalidOperationException("Catalogue products do not have a client policy lifecycle.");
        if (policy.Status == next) return;
        if (policy.Status is PolicyStatus.Cancelled or PolicyStatus.Matured)
            throw new InvalidOperationException($"A {policy.Status.ToString().ToLowerInvariant()} policy cannot change status.");
        if (next == PolicyStatus.Pending)
            throw new InvalidOperationException("An issued policy cannot be returned to Pending.");
        if (policy.Status == PolicyStatus.Pending && next == PolicyStatus.Active && policy.Case?.PolicyIssuedAt is null)
            throw new InvalidOperationException("Complete the application's Policy issued step before activating this policy.");
        if (policy.Status == PolicyStatus.Pending && next is PolicyStatus.Lapsed or PolicyStatus.Matured)
            throw new InvalidOperationException("A pending application can only be issued or cancelled.");
        if (next == PolicyStatus.Matured && (policy.EndDate is null || policy.EndDate.Value.Date > today.Date))
            throw new InvalidOperationException("A policy can only be marked Matured once its recorded end date has been reached.");
    }

    public static IEnumerable<PolicyStatus> AllowedTransitions(Policy policy, DateTime today) =>
        Enum.GetValues<PolicyStatus>().Where(next => next != policy.Status && IsAllowed(policy, next, today));

    private static bool IsAllowed(Policy policy, PolicyStatus next, DateTime today)
    {
        try { ValidateTransition(policy, next, today); return true; }
        catch (InvalidOperationException) { return false; }
    }

    public static void CompleteStep(Case application, CaseStep step, DateTime now)
    {
        if (!Enum.IsDefined(step)) throw new ArgumentException("Choose a valid application step.");
        if (application.Policy.Status is PolicyStatus.Cancelled or PolicyStatus.Matured or PolicyStatus.Lapsed)
            throw new InvalidOperationException($"Application progress cannot change while the policy is {application.Policy.Status}.");
        var times = new DateTime?[] { application.DetailsSubmittedAt, application.AdviserReviewAt,
            application.FicaVerifiedAt, application.SubmittedToLibertyAt, application.PolicyIssuedAt };
        var index = (int)step;
        if (times[index].HasValue) throw new InvalidOperationException($"The step '{step}' has already been completed.");
        if (index > 0 && !times[index - 1].HasValue)
            throw new InvalidOperationException($"Complete '{(CaseStep)(index - 1)}' before '{step}'.");
        switch (step)
        {
            case CaseStep.DetailsSubmitted: application.DetailsSubmittedAt = now; break;
            case CaseStep.AdviserReview: application.AdviserReviewAt = now; break;
            case CaseStep.FicaVerification: application.FicaVerifiedAt = now; break;
            case CaseStep.SubmittedToLiberty:
                application.SubmittedToLibertyAt = now;
                application.Status = CaseStatus.AwaitingApproval;
                break;
            case CaseStep.PolicyIssued:
                application.PolicyIssuedAt = now;
                application.Status = CaseStatus.Completed;
                application.Policy.Status = PolicyStatus.Active;
                application.Policy.UpdatedAt = now;
                break;
        }
        application.UpdatedAt = now;
    }
}
