using Shared.Models;
using Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public static class DbInitializer
    {
        private const string AdvisorEmail = "ratul.singh@liblink.co.za";

        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // ---------------------------------------------------------
            // ADVISOR
            // ---------------------------------------------------------
            var advisor = await context.Advisors
                .FirstOrDefaultAsync(a => a.Email == AdvisorEmail);

            if (advisor == null)
            {
                advisor = new Advisor
                {
                    IdentityProviderSubjectId = "dev-advisor-subject-id",
                    FirstName = "Ratul",
                    LastName = "Singh",
                    Email = AdvisorEmail,
                    Phone = "+27 82 555 0100"
                };

                await context.Advisors.AddAsync(advisor);
                await context.SaveChangesAsync();
            }

            // ---------------------------------------------------------
            // CATALOGUE POLICIES
            // ---------------------------------------------------------
            await EnsureCataloguePolicyAsync(
                context,
                "Retirement Annuity",
                "Liberty",
                "Tax-deferred retirement savings plan.");

            await EnsureCataloguePolicyAsync(
                context,
                "Life Cover",
                "Standard Bank",
                "Lump-sum life cover with optional dread disease rider.");

            await EnsureCataloguePolicyAsync(
                context,
                "Income Protector",
                "Liberty",
                "Monthly income replacement in the event of disability.");

            await EnsureCataloguePolicyAsync(
                context,
                "Investment Plan",
                "Liberty",
                "Long-term investment solution for wealth creation.");

            await EnsureCataloguePolicyAsync(
                context,
                "Tax-Free Savings",
                "Standard Bank",
                "Tax-free savings and investment solution.");

            await EnsureCataloguePolicyAsync(
                context,
                "Disability Cover",
                "Liberty",
                "Protection against loss of income due to disability.");

            await EnsureCataloguePolicyAsync(
                context,
                "Estate Planning",
                "Standard Bank",
                "Estate planning and wealth protection solution.");

            await context.SaveChangesAsync();

            // ---------------------------------------------------------
            // CLIENTS
            // ---------------------------------------------------------
            var clients = new List<Client>
            {
                await EnsureClientAsync(
                    context,
                    advisor,
                    "thandiwe.mokoena@example.com",
                    "Thandiwe",
                    "Mokoena",
                    "+27 82 111 2222",
                    "Moderate",
                    "Employed",
                    30),

                await EnsureClientAsync(
                    context,
                    advisor,
                    "sipho.radebe@example.com",
                    "Sipho",
                    "Radebe",
                    "+27 83 222 3333",
                    "Conservative",
                    "Employed",
                    18),

                await EnsureClientAsync(
                    context,
                    advisor,
                    "naledi.khumalo@example.com",
                    "Naledi",
                    "Khumalo",
                    "+27 84 333 4444",
                    "Aggressive",
                    "Unemployed",
                    8),

                await EnsureClientAsync(
                    context,
                    advisor,
                    "pieter.vanwyk@example.com",
                    "Pieter",
                    "van Wyk",
                    "+27 82 444 5555",
                    "Moderate",
                    "Employed",
                    14),

                await EnsureClientAsync(
                    context,
                    advisor,
                    "aisha.patel@example.com",
                    "Aisha",
                    "Patel",
                    "+27 83 555 6666",
                    "Aggressive",
                    "Employed",
                    10),

                await EnsureClientAsync(
                    context,
                    advisor,
                    "marina.coetzee@example.com",
                    "Marina",
                    "Coetzee",
                    "+27 84 666 7777",
                    "Conservative",
                    "Employed",
                    22),

                await EnsureClientAsync(
                    context,
                    advisor,
                    "johan.steyn@example.com",
                    "Johan",
                    "Steyn",
                    "+27 82 777 8888",
                    "Moderate",
                    "Employed",
                    16),

                await EnsureClientAsync(
                    context,
                    advisor,
                    "sarah.naidoo@example.com",
                    "Sarah",
                    "Naidoo",
                    "+27 83 888 9999",
                    "Moderate",
                    "Employed",
                    6),

                await EnsureClientAsync(
                    context,
                    advisor,
                    "daniel.mthembu@example.com",
                    "Daniel",
                    "Mthembu",
                    "+27 84 999 0000",
                    "Conservative",
                    "Employed",
                    12),

                await EnsureClientAsync(
                    context,
                    advisor,
                    "lerato.dlamini@example.com",
                    "Lerato",
                    "Dlamini",
                    "+27 82 123 4567",
                    "Aggressive",
                    "Employed",
                    4)
            };

            await context.SaveChangesAsync();

            // ---------------------------------------------------------
            // CLIENT POLICIES
            // ---------------------------------------------------------

            // Thandiwe - Retirement Annuity
            var thandiweRA = await EnsureClientPolicyAsync(
                context,
                clients[0],
                "Retirement Annuity",
                "Liberty",
                PolicyStatus.Active,
                premium: 2500,
                cover: null,
                startMonthsAgo: 28,
                endDaysFromNow: 14);

            // Sipho - Life Cover
            var siphoLife = await EnsureClientPolicyAsync(
                context,
                clients[1],
                "Life Cover",
                "Standard Bank",
                PolicyStatus.Active,
                premium: 3200,
                cover: 1_500_000,
                startMonthsAgo: 16,
                endDaysFromNow: 21);

            // Naledi - Income Protector
            var nalediIncome = await EnsureClientPolicyAsync(
                context,
                clients[2],
                "Income Protector",
                "Liberty",
                PolicyStatus.Pending,
                premium: 4100,
                cover: 25_000,
                startMonthsAgo: 1,
                endDaysFromNow: 7);

            // Pieter - Life Cover
            var pieterLife = await EnsureClientPolicyAsync(
                context,
                clients[3],
                "Life Cover",
                "Standard Bank",
                PolicyStatus.Pending,
                premium: 2800,
                cover: 1_000_000,
                startMonthsAgo: 2,
                endDaysFromNow: 30);

            // Aisha - Investment Plan
            var aishaInvestment = await EnsureClientPolicyAsync(
                context,
                clients[4],
                "Investment Plan",
                "Liberty",
                PolicyStatus.Pending,
                premium: 6500,
                cover: null,
                startMonthsAgo: 1,
                endDaysFromNow: 5);

            // Marina - Disability Cover
            var marinaDisability = await EnsureClientPolicyAsync(
                context,
                clients[5],
                "Disability Cover",
                "Liberty",
                PolicyStatus.Pending,
                premium: 3700,
                cover: 2_000_000,
                startMonthsAgo: 2,
                endDaysFromNow: 18);

            // Johan - Estate Planning
            var johanEstate = await EnsureClientPolicyAsync(
                context,
                clients[6],
                "Estate Planning",
                "Standard Bank",
                PolicyStatus.Pending,
                premium: 5200,
                cover: null,
                startMonthsAgo: 1,
                endDaysFromNow: 25);

            // Sarah - Tax-Free Savings
            var sarahSavings = await EnsureClientPolicyAsync(
                context,
                clients[7],
                "Tax-Free Savings",
                "Standard Bank",
                PolicyStatus.Active,
                premium: 4500,
                cover: null,
                startMonthsAgo: 5,
                endDaysFromNow: 12);

            // Daniel - Retirement Annuity
            var danielRA = await EnsureClientPolicyAsync(
                context,
                clients[8],
                "Retirement Annuity",
                "Liberty",
                PolicyStatus.Pending,
                premium: 5800,
                cover: null,
                startMonthsAgo: 1,
                endDaysFromNow: 27);

            // Lerato - Investment Plan
            var leratoInvestment = await EnsureClientPolicyAsync(
                context,
                clients[9],
                "Investment Plan",
                "Liberty",
                PolicyStatus.Pending,
                premium: 6200,
                cover: null,
                startMonthsAgo: 1,
                endDaysFromNow: 9);

            await context.SaveChangesAsync();

            // ---------------------------------------------------------
            // CASES
            // ---------------------------------------------------------
            await EnsureCaseAsync(
                context,
                thandiweRA,
                CaseStatus.AwaitingApproval,
                "Retirement annuity application awaiting approval.");

            await EnsureCaseAsync(
                context,
                siphoLife,
                CaseStatus.InProgress,
                "Life cover application currently being processed.");

            await EnsureCaseAsync(
                context,
                nalediIncome,
                CaseStatus.OnHold,
                "Awaiting client information for income protector application.");

            await EnsureCaseAsync(
                context,
                pieterLife,
                CaseStatus.InProgress,
                "Life cover documentation currently under review.");

            await EnsureCaseAsync(
                context,
                aishaInvestment,
                CaseStatus.OnHold,
                "Additional investment information required from client.");

            await EnsureCaseAsync(
                context,
                marinaDisability,
                CaseStatus.AwaitingApproval,
                "Disability cover awaiting underwriting approval.");

            await EnsureCaseAsync(
                context,
                johanEstate,
                CaseStatus.InProgress,
                "Estate planning consultation and documentation in progress.");

            await EnsureCaseAsync(
                context,
                sarahSavings,
                CaseStatus.AwaitingApproval,
                "Tax-free savings application awaiting final approval.");

            await EnsureCaseAsync(
                context,
                danielRA,
                CaseStatus.InProgress,
                "Retirement annuity application being prepared.");

            await EnsureCaseAsync(
                context,
                leratoInvestment,
                CaseStatus.OnHold,
                "Waiting for client investment documentation.");

            await context.SaveChangesAsync();

            // ---------------------------------------------------------
            // MEETINGS
            // ---------------------------------------------------------
            var today = DateTime.UtcNow.Date;

            // Current week
            await EnsureMeetingAsync(
                context,
                clients[0],
                today.AddHours(10),
                "Portfolio review",
                "Video call");

            await EnsureMeetingAsync(
                context,
                clients[1],
                today.AddDays(1).AddHours(11),
                "Annual check-in",
                "Office");

            await EnsureMeetingAsync(
                context,
                clients[4],
                today.AddDays(2).AddHours(14),
                "Investment consultation",
                "Video call");

            await EnsureMeetingAsync(
                context,
                clients[5],
                today.AddDays(3).AddHours(10),
                "Disability cover review",
                "Office");

            await EnsureMeetingAsync(
                context,
                clients[6],
                today.AddDays(4).AddHours(13),
                "Estate planning consultation",
                "Video call");

            // Previous week - gives percentage comparison data
            await EnsureMeetingAsync(
                context,
                clients[2],
                today.AddDays(-5).AddHours(10),
                "Income protection review",
                "Office");

            await EnsureMeetingAsync(
                context,
                clients[3],
                today.AddDays(-4).AddHours(11),
                "Life cover consultation",
                "Video call");

            // ---------------------------------------------------------
            // CLIENT QUERY
            // ---------------------------------------------------------
            var existingQuery = await context.Queries
                .FirstOrDefaultAsync(q =>
                    q.ClientId == clients[0].ClientId &&
                    q.Subject == "Meeting reschedule");

            if (existingQuery == null)
            {
                await context.Queries.AddAsync(new ClientQuery
                {
                    ClientId = clients[0].ClientId,
                    Subject = "Meeting reschedule",
                    Message = "Can I move my review meeting to next week? I have a work trip.",
                    Status = QueryStatus.Open
                });
            }

            // ---------------------------------------------------------
            // NOTIFICATIONS
            // ---------------------------------------------------------
            var hasAdvisorNotification = await context.Notifications
                .AnyAsync(n =>
                    n.AdvisorId == advisor.AdvisorId &&
                    n.Type == NotificationType.QueryReceived &&
                    n.Message.Contains("Thandiwe Mokoena"));

            if (!hasAdvisorNotification)
            {
                await context.Notifications.AddAsync(new Notification
                {
                    AdvisorId = advisor.AdvisorId,
                    Type = NotificationType.QueryReceived,
                    Message = "Thandiwe Mokoena submitted a query."
                });
            }

            var hasClientNotification = await context.Notifications
                .AnyAsync(n =>
                    n.ClientId == clients[0].ClientId &&
                    n.Type == NotificationType.MeetingConfirmed);

            if (!hasClientNotification)
            {
                await context.Notifications.AddAsync(new Notification
                {
                    ClientId = clients[0].ClientId,
                    Type = NotificationType.MeetingConfirmed,
                    Message = "Your meeting has been confirmed."
                });
            }

            await context.SaveChangesAsync();
        }

        // =============================================================
        // HELPER METHODS
        // =============================================================

        private static async Task EnsureCataloguePolicyAsync(
            ApplicationDbContext context,
            string policyName,
            string provider,
            string description)
        {
            var exists = await context.Policies.AnyAsync(p =>
                p.IsCatalogueItem &&
                p.PolicyName == policyName &&
                p.Provider == provider);

            if (!exists)
            {
                await context.Policies.AddAsync(new Policy
                {
                    PolicyName = policyName,
                    Provider = provider,
                    Description = description,
                    Status = PolicyStatus.Active,
                    IsCatalogueItem = true
                });
            }
        }

        private static async Task<Client> EnsureClientAsync(
            ApplicationDbContext context,
            Advisor advisor,
            string email,
            string firstName,
            string lastName,
            string phone,
            string riskProfile,
            string employmentStatus,
            int createdMonthsAgo)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.Email == email);

            if (client == null)
            {
                client = new Client
                {
                    IdentityProviderSubjectId =
                        $"dev-client-{Guid.NewGuid():N}",

                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Phone = phone,

                    Status = ClientStatus.Active,
                    RiskProfile = riskProfile,
                    AdvisorId = advisor.AdvisorId,
                    EmploymentStatus = employmentStatus,

                    CreatedAt = DateTime.UtcNow.AddMonths(-createdMonthsAgo)
                };

                await context.Clients.AddAsync(client);
                await context.SaveChangesAsync();
            }
            else
            {
                // Repair older records without destroying existing data.
                client.AdvisorId = advisor.AdvisorId;

                if (string.IsNullOrWhiteSpace(client.EmploymentStatus))
                {
                    client.EmploymentStatus = employmentStatus;
                }
            }

            return client;
        }

        private static async Task<Policy> EnsureClientPolicyAsync(
            ApplicationDbContext context,
            Client client,
            string policyName,
            string provider,
            PolicyStatus status,
            double premium,
            double? cover,
            int startMonthsAgo,
            int endDaysFromNow)
        {
            var policy = await context.Policies
                .FirstOrDefaultAsync(p =>
                    p.ClientId == client.ClientId &&
                    !p.IsCatalogueItem &&
                    p.PolicyName == policyName &&
                    p.Provider == provider);

            if (policy == null)
            {
                policy = new Policy
                {
                    ClientId = client.ClientId,
                    PolicyName = policyName,
                    Provider = provider,
                    Status = status,
                    PremiumAmount = premium,
                    CoverAmount = cover,
                    StartDate = DateTime.UtcNow.AddMonths(-startMonthsAgo),
                    EndDate = DateTime.UtcNow.AddDays(endDaysFromNow),
                    IsCatalogueItem = false
                };

                await context.Policies.AddAsync(policy);
                await context.SaveChangesAsync();
            }
            else
            {
                // Make sure older seed records have useful dashboard data.
                if (!policy.EndDate.HasValue)
                {
                    policy.EndDate = DateTime.UtcNow.AddDays(endDaysFromNow);
                }

                if (!policy.PremiumAmount.HasValue ||
                    policy.PremiumAmount.Value <= 0)
                {
                    policy.PremiumAmount = premium;
                }

                if (cover.HasValue && !policy.CoverAmount.HasValue)
                {
                    policy.CoverAmount = cover;
                }
            }

            return policy;
        }

        private static async Task EnsureCaseAsync(
            ApplicationDbContext context,
            Policy policy,
            CaseStatus status,
            string notes)
        {
            var exists = await context.Cases
                .AnyAsync(c => c.PolicyId == policy.PolicyId);

            if (!exists)
            {
                await context.Cases.AddAsync(new Case
                {
                    PolicyId = policy.PolicyId,
                    Status = status,
                    Notes = notes
                });
            }
        }

        private static async Task EnsureMeetingAsync(
            ApplicationDbContext context,
            Client client,
            DateTime meetingDate,
            string meetingType,
            string location)
        {
            var exists = await context.Meetings
                .AnyAsync(m =>
                    m.ClientId == client.ClientId &&
                    m.MeetingDate == meetingDate &&
                    m.MeetingType == meetingType);

            if (!exists)
            {
                await context.Meetings.AddAsync(new Meeting
                {
                    ClientId = client.ClientId,
                    MeetingDate = meetingDate,
                    MeetingType = meetingType,
                    Location = location,
                    Status = MeetingStatus.Confirmed
                });
            }
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
