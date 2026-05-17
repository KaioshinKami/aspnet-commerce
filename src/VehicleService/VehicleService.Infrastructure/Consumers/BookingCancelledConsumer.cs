using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using VehicleService.Application.Services;

namespace VehicleService.Infrastructure.Consumers;

/// <summary>
/// Listens for BookingCancelledEvent and restores vehicle availability.
/// </summary>
public class BookingCancelledConsumer : IConsumer<BookingCancelledEvent>
{
    private readonly VehicleCatalogService _vehicleService;
    private readonly ILogger<BookingCancelledConsumer> _logger;

    public BookingCancelledConsumer(VehicleCatalogService vehicleService, ILogger<BookingCancelledConsumer> logger)
    {
        _vehicleService = vehicleService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookingCancelledEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Received BookingCancelledEvent for vehicle {VehicleId}", evt.VehicleId);
        await _vehicleService.SetAvailabilityAsync(evt.VehicleId, true);
    }
}
