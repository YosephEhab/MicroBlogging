using MediatR;
using MicroBlogging.Domain.Authentication;

namespace MicroBlogging.Application.Users.Commands;

public record RefreshTokenCommand(string RefreshToken, string IpAddress) : IRequest<LoginResponse>;

public class RefreshTokenCommandHandler(IJwtService jwtService) : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var (accessToken, refreshToken) = await jwtService.Refresh(request.RefreshToken, request.IpAddress) ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        return new LoginResponse(
            accessToken,
            refreshToken.Token,
            refreshToken.ExpiresAt
        );
    }
}