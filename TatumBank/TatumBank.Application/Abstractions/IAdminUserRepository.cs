using TatumBank.Domain.Entities;

namespace TatumBank.Application.Abstractions
{
    public interface IAdminUserRepository
    {
        Task<Admin?> GetByUsernameAsync(string username);
        Task<bool> AnyAsync();
        Task CreateAsync(Admin admin);
    }
}
