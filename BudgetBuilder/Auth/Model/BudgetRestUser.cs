using Microsoft.AspNetCore.Identity;

namespace BudgetBuilder.Auth.Model
{
    public class BudgetRestUser : IdentityUser
    {
        public bool forceRelogin { get; set; }

        public string? SupervisorId { get; set; }
    }
}
