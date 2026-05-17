namespace BookingService.Application.DTOs;

public record BookingDto(
    Guid Id,
    Guid VehicleId,
    Guid UserId,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalPrice,
    string Status,
    DateTime CreatedAt);

public record CreateBookingRequest(
    Guid VehicleId,
    DateTime StartDate,
    DateTime EndDate,
    decimal PricePerDay);

public record CancelBookingRequest(string Reason);
