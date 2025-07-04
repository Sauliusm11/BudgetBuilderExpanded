using BudgetBuilder.Auth.Model;
using BudgetBuilder.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuilder.Data
{
    public class BudgetDbContext(IConfiguration configuration) : IdentityDbContext<BudgetRestUser>
    {
        private readonly IConfiguration _configuration = configuration;

        public DbSet<Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Purchase> Purchases { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string? connectionString = _configuration.GetConnectionString("MySql");
            ArgumentNullException.ThrowIfNull(connectionString);
            optionsBuilder.UseMySQL(connectionString);
        }
    }
}
