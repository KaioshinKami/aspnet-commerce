using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using VehicleService.Application.DTOs;
using VehicleService.Application.Interfaces;
using VehicleService.Domain.Entities;

namespace VehicleService.Application.Services;

/// <summary>Core business logic for vehicle catalog management with Redis caching.</summary>
public class VehicleCatalogService
{
    private readonly IVehicleRepository _repo;
    private readonly IDistributedCache _cache;
    private readonly ILogger<VehicleCatalogService> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public VehicleCatalogService(
        IVehicleRepository repo,
        IDistributedCache cache,
        ILogger<VehicleCatalogService> logger)
    {
        _repo = repo;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Get all vehicles with advanced LINQ filtering. Results are cached in Redis.</summary>
    public async Task<List<VehicleDto>> GetAllAsync(VehicleFilterRequest filter)
    {
        var cacheKey = $"vehicles:{JsonSerializer.Serialize(filter)}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            _logger.LogInformation("Cache hit for key {Key}", cacheKey);
            return JsonSerializer.Deserialize<List<VehicleDto>>(cached)!;
        }

        var vehicles = await _repo.GetAllAsync(filter);
        var dtos = vehicles.Select(MapToDto).ToList();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });

        return dtos;
    }

    /// <summary>Get a single vehicle by ID.</summary>
    public async Task<VehicleDto?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"vehicle:{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null)
            return JsonSerializer.Deserialize<VehicleDto>(cached);

        var vehicle = await _repo.GetByIdAsync(id);
        if (vehicle is null) return null;

        var dto = MapToDto(vehicle);
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });

        return dto;
    }

    /// <summary>Create a new vehicle (Admin only).</summary>
    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest req)
    {
        var vehicle = new Vehicle
        {
            Brand = req.Brand,
            Model = req.Model,
            Year = req.Year,
            Transmission = req.Transmission,
            FuelType = req.FuelType,
            Seats = req.Seats,
            PricePerDay = req.PricePerDay,
            ImageUrl = req.ImageUrl,
            Description = req.Description
        };

        await _repo.AddAsync(vehicle);
        await InvalidateCacheAsync();
        return MapToDto(vehicle);
    }

    /// <summary>Update an existing vehicle (Admin only).</summary>
    public async Task<VehicleDto?> UpdateAsync(Guid id, UpdateVehicleRequest req)
    {
        var vehicle = await _repo.GetByIdAsync(id);
        if (vehicle is null) return null;

        vehicle.Brand = req.Brand;
        vehicle.Model = req.Model;
        vehicle.Year = req.Year;
        vehicle.Transmission = req.Transmission;
        vehicle.FuelType = req.FuelType;
        vehicle.Seats = req.Seats;
        vehicle.PricePerDay = req.PricePerDay;
        vehicle.IsAvailable = req.IsAvailable;
        vehicle.ImageUrl = req.ImageUrl;
        vehicle.Description = req.Description;

        await _repo.UpdateAsync(vehicle);
        await _cache.RemoveAsync($"vehicle:{id}");
        await InvalidateCacheAsync();
        return MapToDto(vehicle);
    }

    /// <summary>Delete a vehicle (Admin only).</summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        if (!await _repo.ExistsAsync(id)) return false;
        await _repo.DeleteAsync(id);
        await _cache.RemoveAsync($"vehicle:{id}");
        await InvalidateCacheAsync();
        return true;
    }

    /// <summary>Update vehicle availability — called by event consumer when booking is created/cancelled.</summary>
    public async Task SetAvailabilityAsync(Guid id, bool available)
    {
        await _repo.SetAvailabilityAsync(id, available);
        await _cache.RemoveAsync($"vehicle:{id}");
        await InvalidateCacheAsync();
        _logger.LogInformation("Vehicle {Id} availability set to {Available}", id, available);
    }

    private async Task InvalidateCacheAsync()
    {
        // Invalidate the default "no filter" cache key
        var emptyFilter = new VehicleFilterRequest(null, null, null, null, null, null, null, null);
        var defaultKey = $"vehicles:{JsonSerializer.Serialize(emptyFilter)}";
        await _cache.RemoveAsync(defaultKey);
    }

    private static VehicleDto MapToDto(Vehicle v) => new(
        v.Id, v.Brand, v.Model, v.Year, v.Transmission,
        v.FuelType, v.Seats, v.PricePerDay, v.IsAvailable,
        v.ImageUrl, v.Description);
}
