using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace BookingService.Application.Services;

/// <summary>Core booking business logic with event publishing.</summary>
public class BookingManagementService
{
    private readonly IBookingRepository _repo;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<BookingManagementService> _logger;

    public BookingManagementService(
        IBookingRepository repo,
        IPublishEndpoint publisher,
        ILogger<BookingManagementService> logger)
    {
        _repo = repo;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Creates a booking with full business logic validation:
    /// - dates must be in the future
    /// - end date must be after start date
    /// - minimum 1 day rental
    /// - no overlapping active bookings for the same vehicle
    /// </summary>
    public async Task<BookingDto> CreateAsync(CreateBookingRequest req, Guid userId)
    {
        // Business rule: dates must be valid
        var startDate = DateTime.SpecifyKind(req.StartDate, DateTimeKind.Utc);
        var endDate = DateTime.SpecifyKind(req.EndDate, DateTimeKind.Utc);

        if (startDate >= endDate)
            throw new InvalidOperationException("End date must be after start date.");

        if (startDate < DateTime.UtcNow.Date)
            throw new InvalidOperationException("Start date cannot be in the past.");

        var days = (endDate - startDate).Days;
        if (days < 1)
            throw new InvalidOperationException("Minimum rental period is 1 day.");

        // Business rule: no overlapping bookings
        var hasConflict = await _repo.HasActiveBookingForVehicleAsync(req.VehicleId, startDate, endDate);
        if (hasConflict)
            throw new InvalidOperationException("Vehicle is already booked for the selected dates.");

        var totalPrice = req.PricePerDay * days;

        var booking = new Booking
        {
            VehicleId = req.VehicleId,
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate,
            TotalPrice = totalPrice,
            Status = BookingStatus.Confirmed
        };

        await _repo.AddAsync(booking);

        // Publish event → VehicleService will mark vehicle as unavailable
        await _publisher.Publish(new BookingCreatedEvent(
            booking.Id, booking.VehicleId, booking.UserId,
            booking.StartDate, booking.EndDate, booking.TotalPrice));

        _logger.LogInformation("Booking {Id} created for vehicle {VehicleId}", booking.Id, booking.VehicleId);

        return MapToDto(booking);
    }

    /// <summary>Cancel a booking. Only the owner or Admin can cancel.</summary>
    public async Task<BookingDto> CancelAsync(Guid bookingId, Guid requestingUserId, bool isAdmin, string reason)
    {
        var booking = await _repo.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (!isAdmin && booking.UserId != requestingUserId)
            throw new UnauthorizedAccessException("You can only cancel your own bookings.");

        if (booking.Status == BookingStatus.Cancelled)
            throw new InvalidOperationException("Booking is already cancelled.");

        if (booking.Status == BookingStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed booking.");

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancellationReason = reason;

        await _repo.UpdateAsync(booking);

        // Publish event → VehicleService will restore availability
        await _publisher.Publish(new BookingCancelledEvent(booking.Id, booking.VehicleId, booking.UserId));

        _logger.LogInformation("Booking {Id} cancelled", booking.Id);
        return MapToDto(booking);
    }

    /// <summary>Get all bookings for a specific user.</summary>
    public async Task<List<BookingDto>> GetUserBookingsAsync(Guid userId) =>
        (await _repo.GetByUserIdAsync(userId)).Select(MapToDto).ToList();

    /// <summary>Get all bookings (Admin only).</summary>
    public async Task<List<BookingDto>> GetAllAsync() =>
        (await _repo.GetAllAsync()).Select(MapToDto).ToList();

    /// <summary>Get a single booking by ID.</summary>
    public async Task<BookingDto?> GetByIdAsync(Guid id)
    {
        var b = await _repo.GetByIdAsync(id);
        return b is null ? null : MapToDto(b);
    }

    private static BookingDto MapToDto(Booking b) => new(
        b.Id, b.VehicleId, b.UserId,
        b.StartDate, b.EndDate, b.TotalPrice,
        b.Status.ToString(), b.CreatedAt);
}
