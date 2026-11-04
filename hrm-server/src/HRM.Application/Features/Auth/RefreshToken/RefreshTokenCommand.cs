using MediatR;

namespace HRM.Application.Features.Auth.RefreshToken;

/// <summary>Exchanges the refresh-token cookie for a new access token and a new refresh token.</summary>
public sealed record RefreshTokenCommand(string? RefreshToken) : IRequest<AuthResult>;
