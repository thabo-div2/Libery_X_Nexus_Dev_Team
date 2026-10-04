using API.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;

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

            foreach (var role in new[] { AppRoles.Advisor, AppRoles.Client })
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

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(cEmail) ||
                string.IsNullOrWhiteSpace(cPassword))
            {
                return;
            }

            // ------------------------------------------
            // ADVISOR IDENTITY USER
            // ------------------------------------------
            var user = await userManager.FindByEmailAsync(email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var created = await userManager.CreateAsync(user, password);
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Could not seed advisor login: " +
                        string.Join("; ", created.Errors.Select(e => e.Description)));
                }
            }
            else if (!user.IsActive)
            {
                user.IsActive = true;
                await userManager.UpdateAsync(user);
            }

            if (!await userManager.IsInRoleAsync(user, AppRoles.Advisor))
            {
                await userManager.AddToRoleAsync(user, AppRoles.Advisor);
            }

            // ------------------------------------------
            // ADVISOR DOMAIN RECORD
            // ------------------------------------------
            var advisor = await db.Advisors
                .FirstOrDefaultAsync(a => a.Email == email);

            if (advisor is null)
            {
                advisor = new Advisor
                {
                    Email = email,
                    FirstName = config["Seed:AdvisorFirstName"] ?? "Ratul",
                    LastName = config["Seed:AdvisorLastName"] ?? "Advisor",
                    IdentityProviderSubjectId = user.Id
                };

                db.Advisors.Add(advisor);
                await db.SaveChangesAsync();
            }
            else
            {
                advisor.IdentityProviderSubjectId = user.Id;
            }

            // ------------------------------------------
            // CLIENT IDENTITY USER
            // ------------------------------------------
            var cUser = await userManager.FindByEmailAsync(cEmail);

            if (cUser is null)
            {
                cUser = new ApplicationUser
                {
                    UserName = cEmail,
                    Email = cEmail,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var created = await userManager.CreateAsync(cUser, cPassword);
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Could not seed client login: " +
                        string.Join("; ", created.Errors.Select(e => e.Description)));
                }
            }
            else if (!cUser.IsActive)
            {
                cUser.IsActive = true;
                await userManager.UpdateAsync(cUser);
            }

            if (!await userManager.IsInRoleAsync(cUser, AppRoles.Client))
            {
                await userManager.AddToRoleAsync(cUser, AppRoles.Client);
            }

            // ------------------------------------------
            // CLIENT DOMAIN RECORD
            // ------------------------------------------
            var client = await db.Clients
                .FirstOrDefaultAsync(c => c.Email == cEmail);

            if (client is null)
            {
                client = new Client
                {
                    Email = cEmail,
                    FirstName = config["Seed:ClientFirstName"] ?? "Test",
                    LastName = config["Seed:ClientLastName"] ?? "Client",
                    IdentityProviderSubjectId = cUser.Id,
                    IdentificationNumber = "9001015009087",
                    AdvisorId = advisor.AdvisorId,
                    EmploymentStatus = "Employed",
                    Status = ClientStatus.Active
                };

                db.Clients.Add(client);
            }
            else
            {
                client.IdentityProviderSubjectId = cUser.Id;
                client.AdvisorId = advisor.AdvisorId;

                // EmploymentStatus is required by the Client model/database.
                // Preserve an existing value, but repair old seed records
                // where the column may currently be NULL/empty.
                if (string.IsNullOrWhiteSpace(client.EmploymentStatus))
                {
                    client.EmploymentStatus = "Employed";
                }

                client.Status = ClientStatus.Active;
            }

            await db.SaveChangesAsync();
        }

    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
