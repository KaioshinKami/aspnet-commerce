using IdentityService.Domain.Entities;

namespace IdentityService.Application.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email);
    Task AddAsync(AppUser user);
    Task<bool> ExistsAsync(string email);
}
