using BudgetBuilder.Domain.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace BudgetBuilder.Domain.Data.Entities
{
    public class Company
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public DateTime EstablishedDate { get; set; }

        [Required]
        public required string UserId { get; set; }
        public BudgetRestUser? User { get; set; }
    }


}
