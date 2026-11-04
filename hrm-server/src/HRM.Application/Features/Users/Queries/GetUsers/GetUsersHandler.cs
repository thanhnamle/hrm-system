using HRM.Application.Common.Interfaces;
using MediatR;

namespace HRM.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersHandler(IIdentityService identityService)
    : IRequestHandler<GetUsersQuery, IReadOnlyList<UserAccountDto>>
{
    public Task<IReadOnlyList<UserAccountDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken) =>
        identityService.GetUsersAsync(cancellationToken);
}
