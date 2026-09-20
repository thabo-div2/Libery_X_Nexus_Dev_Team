using API.Data;
using API.Repositories.Implementations;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore; // Required if using DbContext directly
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace API
{
    public class RunTest
    {
        public async static Task SmokeTest(IServiceProvider serviceProvider)
        {

            using var scope = serviceProvider.CreateScope();
            var clientRepo = scope.ServiceProvider.GetRequiredService<IClientRepository>();
            var policyRepo = scope.ServiceProvider.GetRequiredService<IPolicyRepository>();
            var meetingRepo = scope.ServiceProvider.GetRequiredService<IMeetingRepository>();
            var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

            Console.WriteLine("=== Smoke Test: Data Layer ===\n");

            // 1. Basic read
            var clients = (await clientRepo.GetAllAsync()).ToList();
            Console.WriteLine($"[1] Clients found: {clients.Count} (expect 3)");
            foreach (var c in clients)
                Console.WriteLine($"    - {c.FullName} ({c.Status}, risk: {c.RiskProfile})");

            // 2. Filtered/search query
            var searchResult = await clientRepo.SearchAsync(searchTerm: "Mokoena");
            Console.WriteLine($"\n[2] Search 'Mokoena': {searchResult.Count()} match(es) (expect 1)");

            // 3. Related data via navigation / repository join
            if (clients.Any())
            {
                var first = clients.First();
                var details = await clientRepo.GetWithDetailsAsync(first.ClientId);
                Console.WriteLine($"\n[3] {first.FullName} has {details?.Policies.Count ?? 0} policies, " +
                                   $"{details?.Meetings.Count ?? 0} meetings (expect > 0 for at least one client)");
            }

            // 4. Catalogue query (no client attached)
            var catalogue = await policyRepo.GetCatalogoueAsync();
            Console.WriteLine($"\n[4] Catalogue policies: {catalogue.Count()} (expect 3)");

            // 5. Upcoming meetings query (date filtering logic)
            var upcoming = await meetingRepo.GetUpcomingMeetingAsync();
            Console.WriteLine($"\n[5] Upcoming meetings: {upcoming.Count()} (expect 2)");

            // 6. Write path — confirms INSERT + SaveChanges works, not just reads
            await auditRepo.LogAsync(
                userId: null,
                userRole: UserRole.FinancialAdviser,
                actionType: AuditActionType.Login,
                entityAffected: "SmokeTest",
                details: "Smoke test write-path check");
            var recentLogs = await auditRepo.GetRecentAsync(1);
            Console.WriteLine($"\n[6] Audit log write+read: {(recentLogs.Any() ? "OK" : "FAILED")}");

            Console.WriteLine("\n=== If all counts above match expectations, the data layer is working. ===");
        }
    }
}
