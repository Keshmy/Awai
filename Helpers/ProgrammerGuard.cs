using Awai.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Awai.Helpers
{
    public static class ProgrammerGuard
    {
        public static string SeedEmail(IConfiguration config)
            => config["SeedProg:Email"]
               ?? config["SeedAdmin:Email"]
               ?? "admin@awai.ly";

        public static async Task<bool> IsProtectedAsync(UserManager<ApplicationUser> users, ApplicationUser user, IConfiguration config)
        {
            if (await users.IsInRoleAsync(user, "Prog"))
                return true;

            var seed = SeedEmail(config);
            return string.Equals(user.Email, seed, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(user.UserName, seed, StringComparison.OrdinalIgnoreCase);
        }
    }
}
