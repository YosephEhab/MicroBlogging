using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MicroBlogging.Domain.Authentication;
using MicroBlogging.Domain.Entities;
using MicroBlogging.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MicroBlogging.Authentication;

public sealed class JwtService(IRepository<User> userRepository, IRepository<RefreshToken> refreshTokenRepository, IConfiguration config) : IJwtService
{
    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(config["Jwt:AccessTokenLifetimeMinutes"] ?? "15")),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(Guid userId, string ipAddress)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(config["Jwt:RefreshTokenLifetimeDays"] ?? "7")),
            CreatedByIp = ipAddress
        };
    }

    public async Task<(string AccessToken, RefreshToken RefreshToken)> GenerateTokens(User user, string ipAddress)
    {
        var existingTokens = await refreshTokenRepository.GetByCondition(rt => rt.UserId == user.Id && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);
        foreach (var token in existingTokens)
        {
            token.IsRevoked = true;
            token.ReplacedByToken = "replaced-on-login";
            token.MarkUpdated();
            await refreshTokenRepository.Update(token);
        }

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken(user.Id, ipAddress);

        await refreshTokenRepository.Add(refreshToken);
        await refreshTokenRepository.SaveChanges();

        return (accessToken, refreshToken);
    }

    public async Task<(string AccessToken, RefreshToken RefreshToken)?> Refresh(string refreshToken, string ipAddress)
    {
        var existingToken = await refreshTokenRepository.FirstOrDefault(rt => rt.Token == refreshToken);

        if (existingToken is null || existingToken.IsRevoked || existingToken.ExpiresAt <= DateTime.UtcNow)
            return null;

        // rotate token
        existingToken.IsRevoked = true;
        var newToken = GenerateRefreshToken(existingToken.UserId, ipAddress);
        existingToken.ReplacedByToken = newToken.Token;
        existingToken.MarkUpdated();

        await refreshTokenRepository.Update(existingToken);
        await refreshTokenRepository.Add(newToken);
        await refreshTokenRepository.SaveChanges();

        var user = await userRepository.GetById(existingToken.UserId);
        if (user is null) return null;

        var newAccessToken = GenerateAccessToken(user);
        return (newAccessToken, newToken);
    }

    public async Task Revoke(string refreshToken, string ipAddress)
    {
        var token = await refreshTokenRepository.FirstOrDefault(rt => rt.Token == refreshToken);

        if (token is null || token.IsRevoked) return;

        token.IsRevoked = true;
        token.ReplacedByToken = $"revoked-by-{ipAddress}";
        token.MarkUpdated();

        await refreshTokenRepository.Update(token);
        await refreshTokenRepository.SaveChanges();
    }
}
