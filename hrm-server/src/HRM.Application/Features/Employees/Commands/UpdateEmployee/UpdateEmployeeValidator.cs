using FluentValidation;

namespace HRM.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.FirstName).NameRules();
        RuleFor(x => x.LastName).NameRules();
        RuleFor(x => x.Email).EmailRules();
        RuleFor(x => x.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Phone).PhoneRules();
    }
}
