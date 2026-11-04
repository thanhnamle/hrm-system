using HRM.Application.Common.Exceptions;
using HRM.Application.Common.Interfaces;
using MediatR;

namespace HRM.Application.Features.Auth.GetMe;

/// <summary>Reads the account from the database, so a role change shows up without waiting for a new token.</summary>
public sealed class GetMeHandler(ICurrentUser currentUser, IIdentityService identityService)
    : IRequestHandler<GetMeQuery, AuthUserDto>
{
    public async Task<AuthUserDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("Not signed in.");

        var user = await identityService.FindActiveUserAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("Your account can no longer sign in.");

        return AuthUserDto.From(user);
    }
}
