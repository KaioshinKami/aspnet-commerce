using BookingService.Domain.Entities;

namespace BookingService.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<List<Booking>> GetByUserIdAsync(Guid userId);
    Task<List<Booking>> GetAllAsync();
    Task<bool> HasActiveBookingForVehicleAsync(Guid vehicleId, DateTime start, DateTime end);
    Task AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
}
