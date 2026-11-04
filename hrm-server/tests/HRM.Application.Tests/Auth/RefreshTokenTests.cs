using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using HRM.Application.Common.Security;
using HRM.Application.Features.Auth.RefreshToken;
using NSubstitute;

namespace HRM.Application.Tests.Auth;

public class RefreshTokenTests
{
    private static readonly AuthUser Employee =
        new(Guid.CreateVersion7(), "huy.le@hrm.local", Roles.Employee, Guid.CreateVersion7(), "Huy Le");

    private static readonly RefreshTokenRotation Rotation =
        new(Employee.Id, new IssuedRefreshToken("new", DateTimeOffset.UtcNow.AddDays(7)));

    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenService _refreshTokens = Substitute.For<IRefreshTokenService>();

    private RefreshTokenHandler Handler => new(_identity, _jwt, _refreshTokens);

    [Fact]
    public async Task Valid_token_is_rotated_and_a_new_access_token_issued()
    {
        _refreshTokens.RotateAsync("old", Arg.Any<CancellationToken>()).Returns(Rotation);
        _identity.FindActiveUserAsync(Employee.Id, Arg.Any<CancellationToken>()).Returns(Employee);
        _jwt.CreateAccessToken(Employee).Returns(new AccessToken("jwt", DateTimeOffset.UtcNow.AddMinutes(15)));

        var result = await Handler.Handle(new RefreshTokenCommand("old"), CancellationToken.None);

        result.RefreshToken.Token.ShouldBe("new");
        result.AccessToken.Token.ShouldBe("jwt");
        result.User.Id.ShouldBe(Employee.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Missing_cookie_is_unauthorized(string? token)
    {
        await Should.ThrowAsync<UnauthorizedException>(
            () => Handler.Handle(new RefreshTokenCommand(token), CancellationToken.None));

        await _refreshTokens.DidNotReceiveWithAnyArgs().RotateAsync(default!);
    }

    [Fact]
    public async Task Inactive_account_loses_every_session()
    {
        _refreshTokens.RotateAsync("old", Arg.Any<CancellationToken>()).Returns(Rotation);
        _identity.FindActiveUserAsync(Employee.Id, Arg.Any<CancellationToken>()).Returns((AuthUser?)null);

        await Should.ThrowAsync<UnauthorizedException>(
            () => Handler.Handle(new RefreshTokenCommand("old"), CancellationToken.None));

        await _refreshTokens.Received(1).RevokeAllAsync(Employee.Id, Arg.Any<string>(), Arg.Any<CancellationToken>());
        _jwt.DidNotReceiveWithAnyArgs().CreateAccessToken(default!);
    }
}
