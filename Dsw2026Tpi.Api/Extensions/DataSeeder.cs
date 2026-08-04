using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;

namespace Dsw2026Tpi.Api.Extensions
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string[] roleNames = { "ADMINISTRADOR", "PACIENTE" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
            //var adminEmail = "admin@system.com";
            //var adminUser = await userManager.FindByEmailAsync(adminEmail);
            //if (adminUser == null)
            //{
            //    var newAdmin = new IdentityUser
            //    {
            //        UserName = adminEmail,
            //        Email = adminEmail,
            //        EmailConfirmed = true
            //    };
                
            //    var createPowerUser = await userManager.CreateAsync(newAdmin, "Admin1234!");

            //    if (createPowerUser.Succeeded)
            //    {
            //        await userManager.AddToRoleAsync(newAdmin, "ADMINISTRADOR");
            //    }
            //}
        }
    }
}
