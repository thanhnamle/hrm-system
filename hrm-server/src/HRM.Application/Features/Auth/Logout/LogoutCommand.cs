using MediatR;

namespace HRM.Application.Features.Auth.Logout;

public sealed record LogoutCommand(string? RefreshToken) : IRequest;
