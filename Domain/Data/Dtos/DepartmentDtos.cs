using FluentValidation;

namespace BudgetBuilder.Domain.Data.Dtos
{

    public record DepartmentDto(int Id, string Name);
    public record CreateDepartmentDto(string Name);
    public record UpdateDepartmentDto(string Name);
    public record DepartmentReportDto(string Base64);

    public class CreateDepartmentDtoValidator : AbstractValidator<CreateDepartmentDto>
    {
        public CreateDepartmentDtoValidator()
        {
            RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 255);
        }
    }
    public class UpdateDepartmentDtoValidator : AbstractValidator<UpdateDepartmentDto>
    {
        public UpdateDepartmentDtoValidator()
        {
            RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 255);
        }
    }

}
