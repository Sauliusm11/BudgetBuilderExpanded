using FluentValidation;

namespace BudgetBuilder.Data.Dtos
{
    public record CompanyDto(int Id, string Name, DateTime EstablishedDate);
    public record CreateCompanyDto(string Name, DateTime EstablishedDate);
    public record UpdateCompanyDto(string Name, DateTime EstablishedDate);

    public class CreateCompanyDtoValidator : AbstractValidator<CreateCompanyDto>
    {
        public CreateCompanyDtoValidator()
        {
            RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 255);
            RuleFor(dto => dto.EstablishedDate).NotNull();
        }
    }
    public class UpdateCompanyDtoValidator : AbstractValidator<UpdateCompanyDto>
    {
        public UpdateCompanyDtoValidator()
        {
            RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 255);
            RuleFor(dto => dto.EstablishedDate).NotNull();
        }
    }
}
