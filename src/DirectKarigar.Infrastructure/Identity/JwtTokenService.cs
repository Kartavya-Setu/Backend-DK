using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DirectKarigar.Application.Common.Interfaces;
using DirectKarigar.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DirectKarigar.Infrastructure.Identity;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var secret = _configuration["Jwt:Key"] ?? "DirectKarigar_SuperSecretKey_EnterpriseArchitect2026!#$";
        var issuer = _configuration["Jwt:Issuer"] ?? "DirectKarigar.WebApi";
        var audience = _configuration["Jwt:Audience"] ?? "DirectKarigar.Frontend";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString().ToLowerInvariant()),
            new("userId", user.Id),
            new("role", user.Role.ToString().ToLowerInvariant())
        };

        if (user.AdminRole.HasValue)
        {
            claims.Add(new Claim("adminRole", user.AdminRole.Value.ToString().ToLowerInvariant()));
        }

        if (user.ProfessionalProfile != null)
        {
            claims.Add(new Claim("proId", user.ProfessionalProfile.Id));
        }

        if (user.CustomerProfile != null)
        {
            claims.Add(new Claim("customerId", user.CustomerProfile.Id));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
