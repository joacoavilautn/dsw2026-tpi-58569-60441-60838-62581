using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Dsw2026Tpi.Data.Identity;

namespace Dsw2026Tpi.Api.Extensions
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "ADMINISTRADOR", "PACIENTE" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var defaultAdmins = new (string Email, string Password)[]
            {
                ("admin@medportal.com", "admin123"),
                ("admin@system.com", "Admin1234!")
            };

            foreach (var (adminEmail, adminPassword) in defaultAdmins)
            {
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    var newAdmin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var createPowerUser = await userManager.CreateAsync(newAdmin, adminPassword);
                    if (createPowerUser.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newAdmin, "ADMINISTRADOR");
                    }
                }
                else if (!await userManager.IsInRoleAsync(adminUser, "ADMINISTRADOR"))
                {
                    await userManager.AddToRoleAsync(adminUser, "ADMINISTRADOR");
                }
            }
        }
    }
}
