namespace Shared.Contracts.Events;

public record BookingCreatedEvent(
    Guid BookingId,
    Guid VehicleId,
    Guid UserId,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalPrice);
