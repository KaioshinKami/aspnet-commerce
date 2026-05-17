using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _db;
    public UserRepository(IdentityDbContext db) => _db = db;

    public Task<AppUser?> GetByEmailAsync(string email) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(AppUser user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public Task<bool> ExistsAsync(string email) =>
        _db.Users.AnyAsync(u => u.Email == email);
}
