using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TravelSystem.Domain.Entities;

namespace TravelSystem.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // Seed roles
        string[] roles = ["Admin", "PremiumTraveler", "Traveler"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        // Seed admin user
        const string adminEmail = "admin@travelsystem.ao";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new User
            {
                Email = adminEmail,
                UserName = adminEmail,
                FirstName = "Admin",
                LastName = "TravelSystem",
                EmailConfirmed = true,
                PreferredLanguage = "pt-AO"
            };

            var result = await userManager.CreateAsync(admin, "Admin@123456");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
