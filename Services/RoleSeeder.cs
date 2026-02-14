using Microsoft.AspNetCore.Identity;
using Sanalink.API.Models;

namespace Sanalink.API.Services;

public class RoleSeeder : IRoleSeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoleSeeder(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task SeedRolesAsync()
    {
        var roles = new[] { "Admin", "Doctor", "Nurse", "Patient" };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@sanalink.com";
        const string adminPassword = "Admin@123456";

        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
            return;

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Admin",
            Role = "Admin",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
