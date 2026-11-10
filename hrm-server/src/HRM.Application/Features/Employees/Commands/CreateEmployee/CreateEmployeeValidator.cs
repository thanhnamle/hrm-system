using FluentValidation;

namespace HRM.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.FirstName).NameRules();
        RuleFor(x => x.LastName).NameRules();
        RuleFor(x => x.Email).EmailRules();
        RuleFor(x => x.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.HireDate).NotEmpty();
        RuleFor(x => x.Phone).PhoneRules();
        RuleFor(x => x.DateOfBirth).LessThan(x => x.HireDate).When(x => x.DateOfBirth.HasValue);
    }
}
