using BudgetBuilder.Auth.Model;
using Microsoft.AspNetCore.Identity;

namespace BudgetBuilder.Auth
{
    public class AuthDbSeeder
    {
        private readonly UserManager<BudgetRestUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthDbSeeder(UserManager<BudgetRestUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task SeedAsync()
        {
            await AddDefaultRoles();
            await AddAdminUser();
        }

        private async Task AddAdminUser()
        {
            foreach (var role in BudgetRoles.All)
            {
                var roleExists = await _roleManager.RoleExistsAsync(role);
                if(!roleExists) 
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

            var existingAdminUser = await _userManager.FindByNameAsync(newAdminUser.UserName);
            if(existingAdminUser == null) 
            {
                var createAdminUserResult = await _userManager.CreateAsync(newAdminUser, "Admin1!"/*pass; galima pasiimt iš config*/);
                if(createAdminUserResult.Succeeded) 
                {
                    await _userManager.AddToRolesAsync(newAdminUser, BudgetRoles.All);
                }
            }
        }
    }
}
