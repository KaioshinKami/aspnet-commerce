using Microsoft.EntityFrameworkCore;
using VehicleService.Application.DTOs;
using VehicleService.Application.Interfaces;
using VehicleService.Domain.Entities;
using VehicleService.Infrastructure.Persistence;

namespace VehicleService.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly VehicleDbContext _db;
    public VehicleRepository(VehicleDbContext db) => _db = db;

    public Task<Vehicle?> GetByIdAsync(Guid id) =>
        _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id);

    /// <summary>Advanced LINQ filtering — brand, transmission, price range, seats, availability, year.</summary>
    public Task<List<Vehicle>> GetAllAsync(VehicleFilterRequest filter)
    {
        var query = _db.Vehicles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Brand))
            query = query.Where(v => v.Brand.ToLower().Contains(filter.Brand.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.Transmission))
            query = query.Where(v => v.Transmission == filter.Transmission);

        if (!string.IsNullOrWhiteSpace(filter.FuelType))
            query = query.Where(v => v.FuelType == filter.FuelType);

        if (filter.MinPrice.HasValue)
            query = query.Where(v => v.PricePerDay >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(v => v.PricePerDay <= filter.MaxPrice.Value);

        if (filter.MinSeats.HasValue)
            query = query.Where(v => v.Seats >= filter.MinSeats.Value);

        if (filter.IsAvailable.HasValue)
            query = query.Where(v => v.IsAvailable == filter.IsAvailable.Value);

        if (filter.Year.HasValue)
            query = query.Where(v => v.Year == filter.Year.Value);

        return query.OrderBy(v => v.PricePerDay).ToListAsync();
    }

    public async Task AddAsync(Vehicle vehicle)
    {
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Vehicle vehicle)
    {
        _db.Vehicles.Update(vehicle);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var v = await _db.Vehicles.FindAsync(id);
        if (v is not null) { _db.Vehicles.Remove(v); await _db.SaveChangesAsync(); }
    }

    public Task<bool> ExistsAsync(Guid id) =>
        _db.Vehicles.AnyAsync(v => v.Id == id);

    public async Task SetAvailabilityAsync(Guid id, bool available)
    {
        var v = await _db.Vehicles.FindAsync(id);
        if (v is null) return;
        v.IsAvailable = available;
        await _db.SaveChangesAsync();
    }
}
