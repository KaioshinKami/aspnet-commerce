namespace Shared.Contracts.Events;

public record BookingCancelledEvent(
    Guid BookingId,
    Guid VehicleId,
    Guid UserId);
