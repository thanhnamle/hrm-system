using MediatR;

namespace HRM.Application.Features.Auth.GetMe;

public sealed record GetMeQuery : IRequest<AuthUserDto>;
