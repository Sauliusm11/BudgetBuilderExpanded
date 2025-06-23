using BudgetBuilder.Auth.Model;
using BudgetBuilder.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BudgetBuilder.Data
{
    public class BudgetDbContext : IdentityDbContext<BudgetRestUser>
    {
        private readonly IConfiguration _configuration;

        public DbSet<Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Purchase> Purchases { get; set; }


        public BudgetDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL(_configuration.GetConnectionString("MySql"));
        }
    }
}
