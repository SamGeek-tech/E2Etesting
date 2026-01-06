using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OrderService.Application.Services;

/// <summary>
/// Application service for authentication.
/// In a real application, this would use proper user management.
/// </summary>
public class IdentityAppService : IIdentityService
{
    private readonly IConfiguration _configuration;

    // Mock user credentials for testing
    private static readonly Dictionary<string, string> MockUsers = new()
    {
        { "test@example.com", "Password123!" }
    };

    public IdentityAppService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LoginResponse Login(string email, string password)
    {
        // Simple mock authentication
        if (!MockUsers.TryGetValue(email, out var storedPassword) || storedPassword != password)
        {
            return new LoginResponse(false, ErrorMessage: "Invalid credentials");
        }

        var token = GenerateJwtToken(email);
        return new LoginResponse(true, Token: token);
    }

    private string GenerateJwtToken(string email)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "super_secret_key_that_is_long_enough_123";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "OrderService",
            audience: _configuration["Jwt:Audience"] ?? "OrderWeb",
            claims: claims,
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

