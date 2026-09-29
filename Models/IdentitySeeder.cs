using Awai.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Awai.Models
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            string[] roles = ["Prog", "Admin", "Employee", "Customer", "Pending"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var progEmail = config["SeedProg:Email"] ?? "programmer@awai.ly";
            var progPassword = config["SeedProg:Password"];
            var adminEmail = config["SeedAdmin:Email"] ?? "admin@awai.ly";
            var adminPassword = config["SeedAdmin:Password"];

            var programmer = await EnsureUserAsync(userManager, db, progEmail, progPassword, "المبرمج", ["Prog"]);
            await EnsureUserAsync(userManager, db, adminEmail, adminPassword, "مدير النظام", ["Admin"]);

            // Legacy: if old combined admin@awai.ly still has Prog and a separate programmer exists, strip Prog from admin.
            if (!string.Equals(progEmail, adminEmail, StringComparison.OrdinalIgnoreCase))
            {
                var legacy = await userManager.FindByEmailAsync(adminEmail);
                if (legacy != null && await userManager.IsInRoleAsync(legacy, "Prog")
                    && programmer != null && programmer.Id != legacy.Id)
                {
                    await userManager.RemoveFromRoleAsync(legacy, "Prog");
                }
            }
        }

        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            AppDbContext db,
            string email,
            string? password,
            string displayName,
            string[] roles)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                if (string.IsNullOrWhiteSpace(password))
                    return null!;

                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    Age = 30,
                    Approval = true,
                    CreatedDate = DateTime.UtcNow
                };
                await userManager.CreateAsync(user, password);
            }
            else
            {
                user.Approval = true;
                await userManager.UpdateAsync(user);
            }

            foreach (var role in roles)
            {
                if (!await userManager.IsInRoleAsync(user, role))
                    await userManager.AddToRoleAsync(user, role);
            }

            if (!await db.UserProfiles.AnyAsync(p => p.UserId == user.Id))
            {
                db.UserProfiles.Add(new UserProfile
                {
                    DisplayName = displayName,
                    UserId = user.Id
                });
                await db.SaveChangesAsync();
            }

            return user;
        }
    }
}
