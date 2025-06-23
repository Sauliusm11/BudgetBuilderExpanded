using BudgetBuilder.Auth.Model;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace BudgetBuilder.Data.Entities
{
    public class Company
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public DateTime EstablishedDate { get; set; }

        [Required]
        public required string UserId { get; set; }
        public BudgetRestUser User { get; set; }
    }

  
}
