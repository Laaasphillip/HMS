using HMS.Data;
using Microsoft.AspNetCore.Identity;

namespace HMS.Services
{
    /// <summary>
    /// Service to seed initial roles in the database on application startup
    /// </summary>
    public class RoleSeedService
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleSeedService(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        /// <summary>
        /// Seeds the Admin, Staff, and Patient roles if they don't exist
        /// </summary>
        public async Task SeedRolesAsync()
        {
            string[] roles = { "Admin", "Staff", "Patient" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(role));

                    if (result.Succeeded)
                    {
                        Console.WriteLine($"Role '{role}' created successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Error creating role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"Role '{role}' already exists.");
                }
            }
        }
    }
}
