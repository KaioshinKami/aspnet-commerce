namespace VehicleService.Application.DTOs;

public record VehicleDto(
    Guid Id,
    string Brand,
    string Model,
    int Year,
    string Transmission,
    string FuelType,
    int Seats,
    decimal PricePerDay,
    bool IsAvailable,
    string ImageUrl,
    string Description);

public record CreateVehicleRequest(
    string Brand,
    string Model,
    int Year,
    string Transmission,
    string FuelType,
    int Seats,
    decimal PricePerDay,
    string ImageUrl,
    string Description);

public record UpdateVehicleRequest(
    string Brand,
    string Model,
    int Year,
    string Transmission,
    string FuelType,
    int Seats,
    decimal PricePerDay,
    bool IsAvailable,
    string ImageUrl,
    string Description);

public record VehicleFilterRequest(
    string? Brand,
    string? Transmission,
    string? FuelType,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinSeats,
    bool? IsAvailable,
    int? Year);
