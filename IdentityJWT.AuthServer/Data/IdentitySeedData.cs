using IdentityJWT.Models.Configuration;
using IdentityJWT.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace IdentityJWT.API.Data
{
    public static class IdentitySeedData
    {
        public static void CreateIdentitySeedData(IServiceProvider serviceProvider)
        {
            CreateRoles(serviceProvider).GetAwaiter().GetResult();
            CreateAdminAccountAsync(serviceProvider).GetAwaiter().GetResult();
        }

        private static async Task CreateRoles(IServiceProvider serviceProvider)
        {
            serviceProvider = serviceProvider.CreateScope().ServiceProvider;
            RoleManager<AppIdentityRole> roleManager = serviceProvider.GetRequiredService<RoleManager<AppIdentityRole>>();

            var rolesNames = Enum.GetNames(typeof(Roles));
            foreach (string role in rolesNames)
            {
                if (await roleManager.FindByNameAsync(role) == null)
                {
                    await roleManager.CreateAsync(new AppIdentityRole(role));
                }
            }
        }

        public static async Task CreateAdminAccountAsync(IServiceProvider serviceProvider)
        {
            serviceProvider = serviceProvider.CreateScope().ServiceProvider;
            UserManager<AppIdentityUser> userManager = serviceProvider.GetRequiredService<UserManager<AppIdentityUser>>();
            IOptions<IdentitySeedConfiguration> settings = serviceProvider.GetRequiredService<IOptions<IdentitySeedConfiguration>>();

            string username = settings.Value.AdminUserName;
            string email = settings.Value.AdminEmail;
            string password = settings.Value.AdminEmailPassword;

            string firstName = settings.Value.FirstName;
            string lastName = settings.Value.LastName;
            int age = settings.Value.Age;

            if (await userManager.FindByNameAsync(username) == null)
            {
                AppIdentityUser user = new AppIdentityUser
                {
                    UserName = username,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName,
                    Age = age,
                };

                IdentityResult result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    string adminRole = Roles.Admin.ToString();
                    await userManager.AddToRoleAsync(user, adminRole);
                }
            }
        }
    }
}
