using FluentValidation;
using HRM.Application.Common.Models;

namespace HRM.Application.Features.Employees.Queries.GetEmployees;

public sealed class GetEmployeesValidator : PagedQueryValidator<GetEmployeesQuery>
{
    public GetEmployeesValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Status).IsInEnum();
    }
}
