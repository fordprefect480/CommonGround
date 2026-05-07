using CommonGround.Server.Data;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.Auth;

public static class DevSeed
{
    public static async Task SeedAdminAsync(IServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(AppRoles.Admin))
            await roleManager.CreateAsync(new IdentityRole(AppRoles.Admin));

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        const string adminEmail = "admin@local";
        const string adminFirstName = "Site";
        const string adminLastName = "Admin";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = adminFirstName,
                LastName = adminLastName,
            };
            var result = await userManager.CreateAsync(admin, "Password123!");
            if (!result.Succeeded) return;
        }
        else if (string.IsNullOrWhiteSpace(admin.FirstName) && string.IsNullOrWhiteSpace(admin.LastName))
        {
            admin.FirstName = adminFirstName;
            admin.LastName = adminLastName;
            await userManager.UpdateAsync(admin);
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
    }
}
