using FluentValidation;

namespace HRM.Application.Features.Employees;

/// <summary>Field rules shared by the create and update validators. Lengths match the database columns.</summary>
internal static class EmployeeRules
{
    public static void NameRules<T>(this IRuleBuilder<T, string> rule) => rule.NotEmpty().MaximumLength(100);

    public static void EmailRules<T>(this IRuleBuilder<T, string> rule) => rule.NotEmpty().EmailAddress().MaximumLength(256);

    public static void PhoneRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(20).Matches(@"^\+?[0-9 ()-]{6,20}$").WithMessage("'Phone' must contain only digits, spaces, (), - and an optional leading +.");
}
