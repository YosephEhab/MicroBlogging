using MediatR;
using MicroBlogging.Domain.Authentication;
using MicroBlogging.Domain.Entities;
using MicroBlogging.Domain.Repositories;

namespace MicroBlogging.Application.Users.Commands;

public record LoginCommand(string Username, string Password, string IpAddress) : IRequest<LoginResponse>;

public class LoginCommandHandler(IRepository<User> userRepository, IPasswordHasher passwordHasher, IJwtService jwtService) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.FirstOrDefault(u => u.Username == request.Username) ?? throw new UnauthorizedAccessException("Invalid credentials");
        if (!passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
            throw new UnauthorizedAccessException("Invalid credentials");

        var (accessToken, refreshToken) = await jwtService.GenerateTokens(user, request.IpAddress);
        return new LoginResponse(accessToken, refreshToken.Token, refreshToken.ExpiresAt);
    }
}