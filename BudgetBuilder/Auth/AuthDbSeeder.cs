using BudgetBuilder.Auth.Model;
using Microsoft.AspNetCore.Identity;

namespace BudgetBuilder.Auth
{
    public class AuthDbSeeder(UserManager<BudgetRestUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        private readonly UserManager<BudgetRestUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;

        public async Task SeedAsync()
        {
            await AddDefaultRoles();
            await AddAdminUser();
        }

        private async Task AddAdminUser()
        {
            foreach (string role in BudgetRoles.All)
            {
                bool roleExists = await _roleManager.RoleExistsAsync(role);
                if (!roleExists)
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private async Task AddDefaultRoles()
        {
            var newAdminUser = new BudgetRestUser
            {
                UserName = "admin",
                Email = "admin@admin.com"
            };

            BudgetRestUser? existingAdminUser = await _userManager.FindByNameAsync(newAdminUser.UserName);
            if (existingAdminUser == null)
            {
                IdentityResult createAdminUserResult = await _userManager.CreateAsync(newAdminUser, "Admin1!"/*pass; galima pasiimt iš config*/);
                if (createAdminUserResult.Succeeded)
                {
                    await _userManager.AddToRolesAsync(newAdminUser, BudgetRoles.All);
                }
            }
        }
    }
}
