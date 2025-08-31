using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MicroBlogging.Domain.Authentication;

namespace MicroBlogging.Authentication;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddTransient<IJwtService, JwtService>();

        services.AddJwtAuthentication(configuration);
        services.AddAuthorization();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Secret"] ?? "super-secret-key-123";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "MicroBloggingApp";
        var jwtAudience = configuration["Jwt:Audience"] ?? "MicroBloggingAppAudience";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Allow reading token from cookie
                    var token = context.Request.Cookies["AccessToken"];
                    if (!string.IsNullOrEmpty(token))
                        context.Token = token;
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // Suppress the default 401 response
                    context.HandleResponse();

                    // Redirect to login page instead
                    context.Response.Redirect("/Account/Login");
                    return Task.CompletedTask;
                },
                OnForbidden = context =>
                {
                    // Redirect forbidden users as well
                    context.Response.Redirect("/Account/Login");
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}
