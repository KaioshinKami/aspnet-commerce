using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using VehicleService.Application.Services;

namespace VehicleService.Infrastructure.Consumers;

/// <summary>
/// Listens for BookingCreatedEvent from RabbitMQ and marks the vehicle as unavailable.
/// </summary>
public class BookingCreatedConsumer : IConsumer<BookingCreatedEvent>
{
    private readonly VehicleCatalogService _vehicleService;
    private readonly ILogger<BookingCreatedConsumer> _logger;

    public BookingCreatedConsumer(VehicleCatalogService vehicleService, ILogger<BookingCreatedConsumer> logger)
    {
        _vehicleService = vehicleService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookingCreatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Received BookingCreatedEvent for vehicle {VehicleId}", evt.VehicleId);
        await _vehicleService.SetAvailabilityAsync(evt.VehicleId, false);
    }
}
