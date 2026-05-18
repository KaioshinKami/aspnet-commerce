namespace IdentityService.Application.DTOs;

public record RegisterRequest(string FullName, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Email, string Role, Guid UserId);
public record CreateAdminRequest(string FullName, string Email, string Password, string AdminSecretKey);
