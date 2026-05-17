using FluentAssertions;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Application.Services;
using IdentityService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace IdentityService.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SuperSecretKey_CarRental_2024_ChangeMe!",
                ["Jwt:Issuer"] = "CarRentalIdentity",
                ["Jwt:Audience"] = "CarRentalClients"
            })
            .Build();

        _service = new AuthService(_repoMock.Object, config);
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsTokenAndUserInfo()
    {
        // Arrange
        _repoMock.Setup(r => r.ExistsAsync("test@example.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);

        var req = new RegisterRequest("Test User", "test@example.com", "Password123!");

        // Act
        var result = await _service.RegisterAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.Role.Should().Be("User");
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.ExistsAsync("taken@example.com")).ReturnsAsync(true);

        var act = async () => await _service.RegisterAsync(
            new RegisterRequest("User", "taken@example.com", "pass"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var hash = BCrypt.Net.BCrypt.HashPassword("Password123!");
        var user = new AppUser { Email = "user@example.com", PasswordHash = hash, Role = "User", FullName = "Test" };

        _repoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        // Act
        var result = await _service.LoginAsync(new LoginRequest("user@example.com", "Password123!"));

        // Assert
        result.Token.Should().NotBeNullOrEmpty();
        result.Role.Should().Be("User");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");
        var user = new AppUser { Email = "user@example.com", PasswordHash = hash };

        _repoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        var act = async () => await _service.LoginAsync(new LoginRequest("user@example.com", "WrongPassword"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_ThrowsUnauthorized()
    {
        _repoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);

        var act = async () => await _service.LoginAsync(new LoginRequest("nobody@example.com", "pass"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RegisterAsync_AdminRole_AssignsAdminRole()
    {
        _repoMock.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<AppUser>())).Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(
            new RegisterRequest("Admin User", "admin@example.com", "pass", "Admin"));

        result.Role.Should().Be("Admin");
    }
}
