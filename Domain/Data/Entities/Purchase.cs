using BudgetBuilder.Domain.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace BudgetBuilder.Domain.Data.Entities
{
    public class Purchase
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required bool Approved { get; set; }
        public required int Amount { get; set; }
        public required float Cost { get; set; }
        public required DateTime PurchaseDate { get; set; }

        public required Department Department { get; set; }

        [Required]
        public required string UserId { get; set; }
        public BudgetRestUser? User { get; set; }

    }
}
