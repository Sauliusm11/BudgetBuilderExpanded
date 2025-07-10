using Microsoft.AspNetCore.Identity;

namespace BudgetBuilder.Domain.Auth.Model
{
    public class BudgetRestUser : IdentityUser
    {
        public bool ForceRelogin { get; set; }

        public string? SupervisorId { get; set; }
    }
}
