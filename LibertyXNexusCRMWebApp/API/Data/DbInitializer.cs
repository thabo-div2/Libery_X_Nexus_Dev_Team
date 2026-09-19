using Shared.Models;
using Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.Advisors.AnyAsync()) return;

            var advisor = new Advisor
            {
                IdentityProviderSubjectId = "dev-advisor-subject-id",
                FirstName = "Ratul",
                LastName = "Singh",
                Email = "ratul.singh@liblink.co.za",
                Phone = "+27 82 555 0100"
            };
            await context.Advisors.AddAsync(advisor);
            await context.SaveChangesAsync();

            // ---- Catalogue policies (no ClientId — publicly browsable) ----
            var catalogueRA = new Policy
            {
                PolicyName = "Retirement Annuity",
                Provider = "Liberty",
                Description = "Tax-deferred retirement savings plan.",
                Status = PolicyStatus.Active,
                IsCatalogueItem = true
            };
            var catalogueLife = new Policy
            {
                PolicyName = "Life Cover",
                Provider = "Standard Bank",
                Description = "Lump-sum life cover with optional dread disease rider.",
                Status = PolicyStatus.Active,
                IsCatalogueItem = true
            };
            var catalogueIncome = new Policy
            {
                PolicyName = "Income Protector",
                Provider = "Liberty",
                Description = "Monthly income replacement in the event of disability.",
                Status = PolicyStatus.Active,
                IsCatalogueItem = true
            };
            await context.Policies.AddRangeAsync(catalogueRA, catalogueLife, catalogueIncome);
            await context.SaveChangesAsync();

            // ---- Clients ----
            var thandiwe = new Client
            {
                IdentityProviderSubjectId = "dev-client-subject-1",
                FirstName = "Thandiwe",
                LastName = "Mokoena",
                Email = "thandiwe.mokoena@example.com",
                Phone = "+27 82 111 2222",
                Status = ClientStatus.Active,
                RiskProfile = "Moderate",
                AdvisorId = advisor.AdvisorId,
                CreatedAt = DateTime.UtcNow.AddMonths(-30)
            };
            var sipho = new Client
            {
                IdentityProviderSubjectId = "dev-client-subject-2",
                FirstName = "Sipho",
                LastName = "Radebe",
                Email = "sipho.radebe@example.com",
                Phone = "+27 83 222 3333",
                Status = ClientStatus.Active,
                RiskProfile = "Conservative",
                AdvisorId = advisor.AdvisorId,
                CreatedAt = DateTime.UtcNow.AddMonths(-18)
            };
            var naledi = new Client
            {
                IdentityProviderSubjectId = "dev-client-subject-3",
                FirstName = "Naledi",
                LastName = "Khumalo",
                Email = "naledi.khumalo@example.com",
                Phone = "+27 84 333 4444",
                Status = ClientStatus.Active,
                RiskProfile = "Aggressive",
                AdvisorId = advisor.AdvisorId,
                CreatedAt = DateTime.UtcNow.AddMonths(-8)
            };
            await context.Clients.AddRangeAsync(thandiwe, sipho, naledi);
            await context.SaveChangesAsync();

            // ---- Client-held policies ----
            var thandiweRA = new Policy
            {
                ClientId = thandiwe.ClientId,
                PolicyName = "Retirement Annuity",
                Provider = "Liberty",
                Status = PolicyStatus.Active,
                PremiumAmount = 2500.00d,
                StartDate = DateTime.UtcNow.AddMonths(-28),
                IsCatalogueItem = false
            };
            var siphoLife = new Policy
            {
                ClientId = sipho.ClientId,
                PolicyName = "Life Cover",
                Provider = "Standard Bank",
                Status = PolicyStatus.Active,
                CoverAmount = 1500000.00d,
                StartDate = DateTime.UtcNow.AddMonths(-16),
                IsCatalogueItem = false
            };
            var naledIncome = new Policy
            {
                ClientId = naledi.ClientId,
                PolicyName = "Income Protector",
                Provider = "Liberty",
                Status = PolicyStatus.Pending,
                StartDate = DateTime.UtcNow.AddMonths(-1),
                IsCatalogueItem = false
            };
            await context.Policies.AddRangeAsync(thandiweRA, siphoLife, naledIncome);
            await context.SaveChangesAsync();

            // ---- Case for the pending policy amendment ----
            await context.Cases.AddAsync(new Case
            {
                PolicyId = naledIncome.PolicyId,
                Status = CaseStatus.AwaitingApproval,
                Notes = "Awaiting underwriting decision on income protector application."
            });

            // ---- Meetings ----
            await context.Meetings.AddRangeAsync(
                new Meeting
                {
                    ClientId = thandiwe.ClientId,
                    MeetingDate = DateTime.UtcNow.AddDays(3).Date.AddHours(11),
                    MeetingType = "Portfolio review",
                    Location = "Video call",
                    Status = MeetingStatus.Confirmed
                },
                new Meeting
                {
                    ClientId = sipho.ClientId,
                    MeetingDate = DateTime.UtcNow.AddDays(6).Date.AddHours(10),
                    MeetingType = "Annual check-in",
                    Location = "Office",
                    Status = MeetingStatus.Confirmed
                }
            );

            // ---- Query ----
            await context.Queries.AddAsync(new ClientQuery
            {
                ClientId = thandiwe.ClientId,
                Subject = "Meeting reschedule",
                Message = "Can I move my review meeting to next week? I have a work trip.",
                Status = QueryStatus.Open
            });

            // ---- Notifications ----
            await context.Notifications.AddRangeAsync(
                new Notification
                {
                    AdvisorId = advisor.AdvisorId,
                    Type = NotificationType.QueryReceived,
                    Message = "Thandiwe Mokoena submitted a query."
                },
                new Notification
                {
                    ClientId = thandiwe.ClientId,
                    Type = NotificationType.MeetingConfirmed,
                    Message = "Your meeting on Thursday has been confirmed."
                }
            );

            await context.SaveChangesAsync();
        }
    }
}
