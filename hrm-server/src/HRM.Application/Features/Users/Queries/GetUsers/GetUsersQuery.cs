using MediatR;

namespace HRM.Application.Features.Users.Queries.GetUsers;

public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserAccountDto>>;

public sealed record UserAccountDto(Guid Id, string Email, string Role, Guid? EmployeeId, string? FullName, bool IsLockedOut);
