using FluentValidation;

namespace BudgetBuilder.Domain.Data.Dtos
{
    public record PurchaseDto(int Id, string Name, bool Approved, int Amount, float Cost, DateTime PurchaseDate);
    public record CreatePurchaseDto(string Name, int Amount, float Cost, DateTime PurchaseDate);
    public record UpdatePurchaseDto(string Name, int Amount, float Cost, DateTime PurchaseDate);

    public class CreatePurchaseDtoValidator : AbstractValidator<CreatePurchaseDto>
    {
        public CreatePurchaseDtoValidator()
        {
            RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 255);
            RuleFor(dto => dto.Amount).NotEmpty().NotEmpty().GreaterThan(0);
            RuleFor(dto => dto.Cost).NotEmpty().NotEmpty().GreaterThan(0);
            RuleFor(dto => dto.PurchaseDate).NotNull();
        }
    }
    public class UpdatePurchaseDtoValidator : AbstractValidator<UpdatePurchaseDto>
    {
        public UpdatePurchaseDtoValidator()
        {
            RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 255);
            RuleFor(dto => dto.Amount).NotEmpty().NotEmpty().GreaterThan(0);
            RuleFor(dto => dto.Cost).NotEmpty().NotEmpty().GreaterThan(0);
            RuleFor(dto => dto.PurchaseDate).NotNull();
        }
    }
}
