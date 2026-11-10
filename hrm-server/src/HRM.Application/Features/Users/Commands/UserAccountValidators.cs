using FluentValidation;
using HRM.Application.Common.Security;

namespace HRM.Application.Features.Users.Commands;

/// <summary>Length and shape only. Password strength (digits, symbols, ...) is checked by Identity's policy.</summary>
internal static class UserAccountRules
{
    public const int MaxPasswordLength = 128;

    public static void RoleRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().Must(r => Roles.All.Contains(r)).WithMessage($"'Role' must be one of: {string.Join(", ", Roles.All)}.");

    public static void PasswordRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(MaxPasswordLength);
}

public sealed class CreateUserAccountValidator : AbstractValidator<CreateUserAccountCommand>
{
    public CreateUserAccountValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Role).RoleRules();
        RuleFor(x => x.Password).PasswordRules();
    }
}

public sealed class ChangeUserRoleValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleValidator() => RuleFor(x => x.Role).RoleRules();
}

public sealed class ResetUserPasswordValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordValidator() => RuleFor(x => x.NewPassword).PasswordRules();
}
