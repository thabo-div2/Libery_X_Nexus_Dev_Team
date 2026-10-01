using API.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace API.Identity
{
    /// <summary>
    /// Sets up Adviser and Client roles and creates advisor's login account
    /// It also links the advisor's identity account to their Advisor db record
    /// </summary>
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();
            var config = services.GetRequiredService<IConfiguration>();

            foreach (var role in new[] { AppRoles.Advisor, AppRoles.Client})
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var email = config["Seed:AdvisorEmail"];
            var password = config["Seed:AdvisorPassword"];

            var cEmail = config["Seed:ClientEmail"];
            var cPassword = config["Seed:ClientPassword"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return; // There is nothing to seed
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                var created = await userManager.CreateAsync(user, password);

                if (!created.Succeeded)
                {
                    throw new InvalidOperationException("Could not seed advisor login: " + string.Join("; ", created.Errors.Select(e => e.Description)));
                }
                await userManager.AddToRoleAsync(user, AppRoles.Advisor);
            }

            var cUser = await userManager.FindByEmailAsync(cEmail);

            if (cUser is null)
            {
                cUser = new ApplicationUser { UserName = cEmail, Email = cEmail, EmailConfirmed = true };
                var created = await userManager.CreateAsync(cUser, cPassword);

                if (!created.Succeeded)
                {
                    throw new InvalidOperationException("Could not seed client login: " + string.Join("; ", created.Errors.Select(e => e.Description)));
                }
                await userManager.AddToRoleAsync(cUser, AppRoles.Client);
            }

            var advisor = await db.Advisors.FirstOrDefaultAsync(a => a.Email == email);

            if (advisor is null)
            {
                db.Advisors.Add(new Advisor
                {
                    Email = email,
                    FirstName = config["Seed:AdvisorFirstName"] ?? "Ratul",
                    LastName = config["Seed:AdvisorLastName"] ?? "Advisor",
                    IdentityProviderSubjectId = user.Id
                });
            }
            else
            {
                advisor.IdentityProviderSubjectId = user.Id;
            }

            var client = await db.Clients.FirstOrDefaultAsync(c => c.Email == cEmail);

            if (client is null)
            {
                db.Clients.Add(new Client
                {
                    Email = cEmail,
                    FirstName = "Test",
                    LastName = "Test",
                    IdentityProviderSubjectId = cUser.Id,
                    IdentificationNumber = "9001015009087",
                    AdvisorId = advisor?.AdvisorId
                });
            }
            else
            {
                client.IdentityProviderSubjectId = cUser.Id;
                client.AdvisorId = advisor?.AdvisorId;
            }

            await db.SaveChangesAsync();
        }
        
    }
}
