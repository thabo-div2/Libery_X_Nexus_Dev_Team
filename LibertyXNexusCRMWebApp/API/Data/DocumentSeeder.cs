using API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;
using System.Text;

namespace API.Data
{
    /// <summary>
    /// DEVELOPMENT ONLY. Creates a few sample documents that are "awaiting signature":
    /// a small PDF is uploaded to blob storage and a matching row is inserted into the Documents table.
    ///
    /// Safe to run repeatedly: a document is skipped if one with the same client + file name already exists
    /// (seeded rows are tagged with UploadedBy = "seed-data"). Use RemoveAsync to delete them again
    /// (both the SQL rows and the blobs).
    /// </summary>
    public static class DocumentSeeder
    {
        /// <summary>Tag stored in Document.UploadedBy so seeded rows can be found and removed.</summary>
        public const string SeedMarker = "seed-data";

        private sealed record SampleDocument(DocumentType Type, string FileName, string Heading, string Summary);

        private static readonly SampleDocument[] Samples =
        {
            new(DocumentType.AmendmentForm,
                "Amendment Form - Beneficiary Update.pdf",
                "Amendment Form - Beneficiary Update",
                "Request to update the nominated beneficiary on this policy."),

            new(DocumentType.PolicyStatement,
                "Policy Statement - Annual Review.pdf",
                "Policy Statement - Annual Review",
                "Annual statement of cover, premiums and benefits for your review and signature."),

            new(DocumentType.ServicePackageSummary,
                "Service Package Summary.pdf",
                "Service Package Summary",
                "Summary of the advisory service package agreed with your advisor."),

            new(DocumentType.AmendmentForm,
                "Amendment Form - Premium Change.pdf",
                "Amendment Form - Premium Change",
                "Request to change the monthly premium on this policy.")
        };

        /// <summary>
        /// Uploads sample PDFs and inserts matching Document rows marked as awaiting signature.
        /// Never throws: a failure (for example, blob storage not reachable) is logged and startup continues.
        /// </summary>
        public static async Task SeedAsync(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DocumentSeeder));
            var db = services.GetRequiredService<ApplicationDbContext>();
            var blobs = services.GetRequiredService<IBlobStorageService>();

            try
            {
                var clients = await db.Clients
                    .Where(c => c.AdvisorId != null)
                    .OrderBy(c => c.ClientId)
                    .Take(Samples.Length)
                    .ToListAsync();

                if (clients.Count == 0)
                {
                    logger.LogInformation("Sample documents skipped: no clients with an advisor were found.");
                    return;
                }

                var created = 0;

                for (var i = 0; i < Samples.Length; i++)
                {
                    var client = clients[i % clients.Count];
                    var sample = Samples[i];

                    var alreadySeeded = await db.Documents.AnyAsync(d =>
                        d.ClientId == client.ClientId &&
                        d.FileName == sample.FileName &&
                        d.UploadedBy == SeedMarker);

                    if (alreadySeeded)
                    {
                        continue;
                    }

                    // Documents must belong to a policy, so use the client's first real (non-catalogue) policy.
                    var policy = await db.Policies
                        .Where(p => p.ClientId == client.ClientId && !p.IsCatalogueItem)
                        .OrderBy(p => p.PolicyId)
                        .FirstOrDefaultAsync();

                    if (policy is null)
                    {
                        continue;
                    }

                    var pdf = BuildPdf(sample.Heading, new[]
                    {
                        sample.Summary,
                        $"Client: {client.FullName}",
                        $"Policy: {policy.PolicyName} ({policy.Provider})",
                        "Status: Awaiting signature",
                        "",
                        "SAMPLE DOCUMENT - generated for development and testing only."
                    });

                    string blobReference;
                    using (var stream = new MemoryStream(pdf))
                    {
                        blobReference = await blobs.UploadAsync(stream, sample.FileName, "application/pdf");
                    }

                    try
                    {
                        db.Documents.Add(new Document
                        {
                            ClientId = client.ClientId,
                            PolicyId = policy.PolicyId,
                            FileName = sample.FileName,
                            BlobReference = blobReference,
                            ContentType = "application/pdf",
                            FileSizeBytes = pdf.Length,
                            DocumentType = sample.Type,
                            VisibleToClient = true,
                            SignatureStatus = SignatureStatus.Awaiting,
                            UpdateAt = DateTime.UtcNow.AddDays(-(i + 1)),
                            UploadedBy = SeedMarker
                        });

                        await db.SaveChangesAsync();
                        created++;
                    }
                    catch
                    {
                        // Don't leave an orphaned blob behind if the database insert failed.
                        await blobs.DeleteAsync(blobReference);
                        throw;
                    }
                }

                logger.LogInformation("Sample documents: {Created} created, {Skipped} already present.",
                    created, Samples.Length - created);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Sample document seeding was skipped: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Deletes every seeded document: the blob first, then the SQL row.
        /// </summary>
        public static async Task RemoveAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var blobs = services.GetRequiredService<IBlobStorageService>();

            var seeded = await db.Documents
                .Where(d => d.UploadedBy == SeedMarker)
                .ToListAsync();

            foreach (var document in seeded)
            {
                await blobs.DeleteAsync(document.BlobReference);
            }

            db.Documents.RemoveRange(seeded);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Builds a minimal, valid one-page PDF (ASCII only) so the Download button opens a real file.
        /// </summary>
        private static byte[] BuildPdf(string title, IEnumerable<string> lines)
        {
            // Keeps the PDF ASCII-only (byte offsets in the xref table depend on it) and escapes PDF string characters.
            static string Escape(string text)
            {
                var ascii = new string(text.Select(c => c < 128 ? c : '?').ToArray());
                return ascii.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            }

            var content = new StringBuilder();
            content.Append($"BT /F1 18 Tf 72 790 Td ({Escape(title)}) Tj ET\n");

            var y = 755;
            foreach (var line in lines)
            {
                content.Append($"BT /F1 11 Tf 72 {y} Td ({Escape(line)}) Tj ET\n");
                y -= 18;
            }

            var contentText = content.ToString();

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
                $"<< /Length {Encoding.ASCII.GetByteCount(contentText)} >>\nstream\n{contentText}endstream",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
            };

            var pdf = new StringBuilder("%PDF-1.4\n");
            var offsets = new List<int>();

            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(pdf.Length);
                pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xrefStart = pdf.Length;
            pdf.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");

            foreach (var offset in offsets)
            {
                pdf.Append($"{offset:D10} 00000 n \n");
            }

            pdf.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefStart}\n%%EOF\n");

            return Encoding.ASCII.GetBytes(pdf.ToString());
        }
    }
}