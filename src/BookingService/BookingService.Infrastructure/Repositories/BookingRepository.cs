using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _db;
    public BookingRepository(BookingDbContext db) => _db = db;

    public Task<Booking?> GetByIdAsync(Guid id) =>
        _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);

    public Task<List<Booking>> GetByUserIdAsync(Guid userId) =>
        _db.Bookings.Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt).ToListAsync();

    public Task<List<Booking>> GetAllAsync() =>
        _db.Bookings.OrderByDescending(b => b.CreatedAt).ToListAsync();

    /// <summary>Check for overlapping active bookings for the same vehicle.</summary>
    public Task<bool> HasActiveBookingForVehicleAsync(Guid vehicleId, DateTime start, DateTime end) =>
        _db.Bookings.AnyAsync(b =>
            b.VehicleId == vehicleId &&
            b.Status != BookingStatus.Cancelled &&
            b.StartDate < end &&
            b.EndDate > start);

    public async Task AddAsync(Booking booking)
    {
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Booking booking)
    {
        _db.Bookings.Update(booking);
        await _db.SaveChangesAsync();
    }
}
