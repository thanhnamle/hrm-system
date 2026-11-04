using FluentValidation;
using HRM.Application.Common.Behaviors;
using HRM.Application.Features.Auth.Login;
using ValidationException = HRM.Application.Common.Exceptions.ValidationException;

namespace HRM.Application.Tests.Common;

public class ValidationBehaviorTests
{
    private static readonly IValidator<LoginCommand>[] Validators = [new LoginValidator()];

    [Fact]
    public async Task Invalid_request_throws_with_field_errors_and_skips_handler()
    {
        var behavior = new ValidationBehavior<LoginCommand, string>(Validators);
        var handlerCalled = false;

        var ex = await Should.ThrowAsync<ValidationException>(() => behavior.Handle(
            new LoginCommand("not-an-email", ""),
            _ => { handlerCalled = true; return Task.FromResult("ok"); },
            CancellationToken.None));

        ex.Errors.Keys.ShouldBe(["Email", "Password"], ignoreOrder: true);
        handlerCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Valid_request_reaches_handler()
    {
        var behavior = new ValidationBehavior<LoginCommand, string>(Validators);

        var result = await behavior.Handle(
            new LoginCommand("a@hrm.local", "pw"), _ => Task.FromResult("ok"), CancellationToken.None);

        result.ShouldBe("ok");
    }
}
