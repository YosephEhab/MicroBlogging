namespace MicroBlogging.Application.Users.Commands;

public record LoginResponse(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
