using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class BackfillReplacementRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older rejected uploads were not always linked to a checklist request.
            // One request per client/type/policy is enough; preserve existing outstanding requests.
            migrationBuilder.Sql("""
                WITH Rejected AS (
                    SELECT d.*, ROW_NUMBER() OVER (
                        PARTITION BY d.ClientId, d.PolicyId, d.DocumentType
                        ORDER BY d.ReviewedAt DESC, d.DocumentId DESC) AS rn
                    FROM Documents d
                    WHERE d.ReviewStatus = 2 AND d.VisibleToClient = 1
                )
                INSERT INTO DocumentRequests
                    (ClientId, PolicyId, DocumentType, Note, CreatedAt, FulfilledAt, FulfilledDocumentId, ReviewStatus, ReviewNote)
                SELECT d.ClientId, d.PolicyId, d.DocumentType,
                    N'Please upload a corrected replacement document.', GETUTCDATE(), NULL, NULL, 2, d.ReviewNote
                FROM Rejected d
                WHERE d.rn = 1
                AND NOT EXISTS (
                    SELECT 1 FROM Documents newer
                    WHERE newer.ClientId = d.ClientId AND newer.DocumentType = d.DocumentType
                        AND (newer.PolicyId = d.PolicyId OR (newer.PolicyId IS NULL AND d.PolicyId IS NULL))
                        AND newer.DocumentId > d.DocumentId AND newer.ReviewStatus <> 2
                )
                AND NOT EXISTS (
                    SELECT 1 FROM DocumentRequests r
                    WHERE r.ClientId = d.ClientId AND r.DocumentType = d.DocumentType
                        AND (r.PolicyId = d.PolicyId OR (r.PolicyId IS NULL AND d.PolicyId IS NULL))
                        AND r.FulfilledAt IS NULL
                );
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep repaired requests: they may already have been acted on by the client.

        }
    }
}
