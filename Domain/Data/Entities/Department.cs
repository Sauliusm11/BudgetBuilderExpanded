using BudgetBuilder.Domain.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace BudgetBuilder.Domain.Data.Entities
{
    public class Department
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required Company Company { get; set; }

        [Required]
        public required string UserId { get; set; }
        public BudgetRestUser? User { get; set; }
    }
}
