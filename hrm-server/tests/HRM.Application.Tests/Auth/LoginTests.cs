using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using HRM.Application.Features.Auth.Login;
using NSubstitute;

namespace HRM.Application.Tests.Auth;

public class LoginTests
{
    private static readonly AuthUser Manager =
        new(Guid.CreateVersion7(), "minh.nguyen@hrm.local", Roles.Manager, Guid.CreateVersion7(), "Minh Nguyen");

    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenService _refreshTokens = Substitute.For<IRefreshTokenService>();

    private LoginHandler Handler => new(_identity, _jwt, _refreshTokens);

    [Fact]
    public async Task Valid_credentials_return_tokens_and_role_permissions()
    {
        var access = new AccessToken("jwt", DateTimeOffset.UtcNow.AddMinutes(15));
        var refresh = new IssuedRefreshToken("refresh", DateTimeOffset.UtcNow.AddDays(7));
        _identity.AuthenticateAsync(Manager.Email, "pw", Arg.Any<CancellationToken>()).Returns(Manager);
        _jwt.CreateAccessToken(Manager).Returns(access);
        _refreshTokens.IssueAsync(Manager.Id, Arg.Any<CancellationToken>()).Returns(refresh);

        var result = await Handler.Handle(new LoginCommand(Manager.Email, "pw"), CancellationToken.None);

        result.AccessToken.ShouldBe(access);
        result.RefreshToken.ShouldBe(refresh);
        result.User.Role.ShouldBe(Roles.Manager);
        result.User.Permissions.ShouldBe(RolePermissions.For(Roles.Manager));
    }

    [Fact]
    public async Task Rejected_credentials_throw_generic_unauthorized_and_issue_nothing()
    {
        _identity.AuthenticateAsync(default!, default!, default).ReturnsForAnyArgs((AuthUser?)null);

        var ex = await Should.ThrowAsync<UnauthorizedException>(
            () => Handler.Handle(new LoginCommand("x@hrm.local", "wrong"), CancellationToken.None));

        ex.Message.ShouldBe(LoginHandler.InvalidCredentials);
        _jwt.DidNotReceiveWithAnyArgs().CreateAccessToken(default!);
        await _refreshTokens.DidNotReceiveWithAnyArgs().IssueAsync(default);
    }

    [Theory]
    [InlineData("", "pw", "Email")]
    [InlineData("not-an-email", "pw", "Email")]
    [InlineData("a@hrm.local", "", "Password")]
    public void Validator_rejects_bad_input(string email, string password, string field)
    {
        var result = new LoginValidator().Validate(new LoginCommand(email, password));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == field);
    }

    [Fact]
    public void Validator_accepts_good_input() =>
        new LoginValidator().Validate(new LoginCommand("a@hrm.local", "pw")).IsValid.ShouldBeTrue();
}
