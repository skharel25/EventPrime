using EventPrime.Api.Models;

namespace EventPrime.Api.Services;

public interface IUserStore
{
    Task<UserEntity?> GetByEmailAsync(string email);
    Task CreateAsync(UserEntity user);
    Task SeedAdminUserAsync();
}
