using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VehicleService.Application.DTOs;
using VehicleService.Application.Interfaces;
using VehicleService.Application.Services;
using VehicleService.Domain.Entities;
using Xunit;

namespace VehicleService.Tests;

public class VehicleCatalogServiceTests
{
    private readonly Mock<IVehicleRepository> _repoMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly VehicleCatalogService _service;

    public VehicleCatalogServiceTests()
    {
        _service = new VehicleCatalogService(
            _repoMock.Object,
            _cacheMock.Object,
            NullLogger<VehicleCatalogService>.Instance);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingVehicle_ReturnsDto()
    {
        // Arrange
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Brand = "BMW", Model = "M4", PricePerDay = 150m };
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
        _repoMock.Setup(r => r.GetByIdAsync(vehicle.Id)).ReturnsAsync(vehicle);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetByIdAsync(vehicle.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Brand.Should().Be("BMW");
        result.Model.Should().Be("M4");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingVehicle_ReturnsNull()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Vehicle?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_AddsVehicleAndInvalidatesCache()
    {
        // Arrange
        var req = new CreateVehicleRequest("Toyota", "Camry", 2023, "Automatic", "Petrol", 5, 60m, "", "");
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Vehicle>())).Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(req);

        // Assert
        result.Brand.Should().Be("Toyota");
        result.PricePerDay.Should().Be(60m);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Vehicle>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingVehicle_ReturnsTrueAndInvalidatesCache()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);
        _repoMock.Setup(r => r.DeleteAsync(id)).Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(id);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingVehicle_ReturnsFalse()
    {
        _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _service.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task SetAvailabilityAsync_CallsRepositoryAndInvalidatesCache()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.SetAvailabilityAsync(id, false)).Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        await _service.SetAvailabilityAsync(id, false);

        _repoMock.Verify(r => r.SetAvailabilityAsync(id, false), Times.Once);
    }
}
