using MediatR;

namespace HRM.Application.Features.Users.Commands;

/// <summary>The login email is taken from the employee record, so the two cannot drift apart.</summary>
public sealed record CreateUserAccountCommand(Guid EmployeeId, string Role, string Password) : IRequest<Guid>;

/// <summary>Takes effect on the user's next sign-in; their current sessions are revoked.</summary>
public sealed record ChangeUserRoleCommand(Guid UserId, string Role) : IRequest;

public sealed record LockUserCommand(Guid UserId) : IRequest;

public sealed record UnlockUserCommand(Guid UserId) : IRequest;

public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest;
