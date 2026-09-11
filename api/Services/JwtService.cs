using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Testify.Api.Services;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string username);
    (Guid UserId, string Email, string Username)? ValidateToken(string token);
}

public class JwtService : IJwtService
{
    private readonly string _secretKey;
    private readonly int _expirationMinutes = 15;

    public JwtService(IConfiguration configuration)
    {
        _secretKey = configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET not configured");
    }

    public string GenerateAccessToken(Guid userId, string email, string username)
    {
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, username)
        };

        var token = new JwtSecurityToken(
            issuer: "Testify.Api",
            audience: "Testify.Web",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (Guid UserId, string Email, string Username)? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_secretKey));
            var handler = new JwtSecurityTokenHandler();

            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = "Testify.Api",
                ValidateAudience = true,
                ValidAudience = "Testify.Web",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
            var usernameClaim = principal.FindFirst(ClaimTypes.Name)?.Value;

            if (userIdClaim == null || emailClaim == null || usernameClaim == null)
                return null;

            return (Guid.Parse(userIdClaim), emailClaim, usernameClaim);
        }
        catch
        {
            return null;
        }
    }
}