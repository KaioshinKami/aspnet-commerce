using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Application.Services;
using BookingService.Domain.Entities;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Contracts.Events;
using Xunit;

namespace BookingService.Tests;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _repoMock = new();
    private readonly Mock<IPublishEndpoint> _publisherMock = new();
    private readonly BookingManagementService _service;

    public BookingServiceTests()
    {
        _service = new BookingManagementService(
            _repoMock.Object,
            _publisherMock.Object,
            NullLogger<BookingManagementService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsBookingDto()
    {
        // Arrange
        var req = new CreateBookingRequest(
            Guid.NewGuid(),
            DateTime.UtcNow.Date.AddDays(1),
            DateTime.UtcNow.Date.AddDays(4),
            50m);

        _repoMock.Setup(r => r.HasActiveBookingForVehicleAsync(req.VehicleId, req.StartDate, req.EndDate))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.Publish(It.IsAny<BookingCreatedEvent>(), default)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(req, Guid.NewGuid());

        // Assert
        result.Should().NotBeNull();
        result.TotalPrice.Should().Be(150m); // 3 days * 50
        result.Status.Should().Be("Confirmed");
        _publisherMock.Verify(p => p.Publish(It.IsAny<BookingCreatedEvent>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EndDateBeforeStartDate_ThrowsInvalidOperation()
    {
        // Arrange
        var req = new CreateBookingRequest(
            Guid.NewGuid(),
            DateTime.UtcNow.Date.AddDays(5),
            DateTime.UtcNow.Date.AddDays(2),
            50m);

        // Act
        var act = async () => await _service.CreateAsync(req, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*End date must be after start date*");
    }

    [Fact]
    public async Task CreateAsync_StartDateInPast_ThrowsInvalidOperation()
    {
        // Arrange
        var req = new CreateBookingRequest(
            Guid.NewGuid(),
            DateTime.UtcNow.Date.AddDays(-2),
            DateTime.UtcNow.Date.AddDays(2),
            50m);

        // Act
        var act = async () => await _service.CreateAsync(req, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*past*");
    }

    [Fact]
    public async Task CreateAsync_VehicleAlreadyBooked_ThrowsInvalidOperation()
    {
        // Arrange
        var req = new CreateBookingRequest(
            Guid.NewGuid(),
            DateTime.UtcNow.Date.AddDays(1),
            DateTime.UtcNow.Date.AddDays(4),
            50m);

        _repoMock.Setup(r => r.HasActiveBookingForVehicleAsync(req.VehicleId, req.StartDate, req.EndDate))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _service.CreateAsync(req, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already booked*");
    }

    [Fact]
    public async Task CancelAsync_OwnBooking_CancelsSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            UserId = userId,
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(5),
            Status = BookingStatus.Confirmed
        };

        _repoMock.Setup(r => r.GetByIdAsync(booking.Id)).ReturnsAsync(booking);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.Publish(It.IsAny<BookingCancelledEvent>(), default)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelAsync(booking.Id, userId, false, "Changed plans");

        // Assert
        result.Status.Should().Be("Cancelled");
        _publisherMock.Verify(p => p.Publish(It.IsAny<BookingCancelledEvent>(), default), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_OtherUserBooking_ThrowsUnauthorized()
    {
        // Arrange
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = BookingStatus.Confirmed
        };

        _repoMock.Setup(r => r.GetByIdAsync(booking.Id)).ReturnsAsync(booking);

        // Act
        var act = async () => await _service.CancelAsync(booking.Id, Guid.NewGuid(), false, "reason");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ThrowsInvalidOperation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = BookingStatus.Cancelled
        };

        _repoMock.Setup(r => r.GetByIdAsync(booking.Id)).ReturnsAsync(booking);

        // Act
        var act = async () => await _service.CancelAsync(booking.Id, userId, false, "reason");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already cancelled*");
    }

    [Fact]
    public async Task CancelAsync_NotFound_ThrowsKeyNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Booking?)null);

        var act = async () => await _service.CancelAsync(Guid.NewGuid(), Guid.NewGuid(), false, "reason");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
