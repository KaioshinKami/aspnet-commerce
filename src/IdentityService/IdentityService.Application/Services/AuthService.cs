using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Application.Services;

/// <summary>Handles user registration and JWT token generation.</summary>
public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository users, IConfiguration config)
    {
        _users = users;
        _config = config;
    }

    /// <summary>Registers a new user. Throws if email already taken.</summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        if (await _users.ExistsAsync(req.Email))
            throw new InvalidOperationException("Email already registered.");

        // Only allow Admin role if explicitly set (could add admin-key check here)
        var role = req.Role == "Admin" ? "Admin" : "User";

        var user = new AppUser
        {
            Email = req.Email,
            FullName = req.FullName,
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        await _users.AddAsync(user);
        return new AuthResponse(GenerateToken(user), user.Email, user.Role, user.Id);
    }

    /// <summary>Validates credentials and returns a JWT token.</summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return new AuthResponse(GenerateToken(user), user.Email, user.Role, user.Id);
    }

    private string GenerateToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
