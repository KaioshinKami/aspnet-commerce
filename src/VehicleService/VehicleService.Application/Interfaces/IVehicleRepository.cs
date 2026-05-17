using VehicleService.Application.DTOs;
using VehicleService.Domain.Entities;

namespace VehicleService.Application.Interfaces;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id);
    Task<List<Vehicle>> GetAllAsync(VehicleFilterRequest filter);
    Task AddAsync(Vehicle vehicle);
    Task UpdateAsync(Vehicle vehicle);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task SetAvailabilityAsync(Guid id, bool available);
}
