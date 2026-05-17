namespace IdentityService.Application.DTOs;

public record RegisterRequest(string FullName, string Email, string Password, string Role = "User");
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Email, string Role, Guid UserId);
