using MicroBlogging.Domain.Entities;

namespace MicroBlogging.Domain.Authentication;

public interface IJwtService
{
    Task<(string AccessToken, RefreshToken RefreshToken)> GenerateTokens(User user, string ipAddress);
    Task<(string AccessToken, RefreshToken RefreshToken)?> Refresh(string refreshToken, string ipAddress);
    Task Revoke(string refreshToken, string ipAddress);
}