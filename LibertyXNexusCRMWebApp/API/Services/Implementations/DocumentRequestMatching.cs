using Shared.Models;

namespace API.Services.Implementations;

public static class DocumentRequestMatching
{
    // Exact scope matching: general uploads never complete policy requests, or vice versa.
    public static IQueryable<DocumentRequest> MatchingOutstanding(this IQueryable<DocumentRequest> requests, Document document) =>
        requests.Where(r => r.ClientId == document.ClientId && r.DocumentType == document.DocumentType
            && r.PolicyId == document.PolicyId && r.FulfilledAt == null);
}
