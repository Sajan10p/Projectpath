using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projectpath.Models;

namespace Projectpath.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // ================= ROLES =================
            string[] roles = { "Admin", "Student", "Tutor", "Company" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ================= ADMIN =================
            // ================= ADMIN =================
var adminEmail = "admin@projectpath.com";
var adminUser = await userManager.FindByEmailAsync(adminEmail);

if (adminUser == null)
{
    adminUser = new ApplicationUser
    {
        FullName = "System Admin",
        UserName = adminEmail,
        Email = adminEmail,
        UserRole = "Admin",
        EmailConfirmed = true,
        IsActive = true,
        IsRegistrationApproved = true,
        RegistrationStatus = "Approved",
        RegistrationApprovedAt = DateTime.Now
    };

    var result = await userManager.CreateAsync(adminUser, "Password1!");

    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
else
{
    adminUser.EmailConfirmed = true;
    adminUser.IsActive = true;
    adminUser.IsRegistrationApproved = true;
    adminUser.RegistrationStatus = "Approved";
    adminUser.RegistrationApprovedAt ??= DateTime.Now;
    adminUser.UserRole = "Admin";

    await userManager.UpdateAsync(adminUser);

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
        }
    }
}
