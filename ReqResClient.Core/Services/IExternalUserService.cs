using ReqResClient.Core.Models;

namespace ReqResClient.Core.Services;

public interface IExternalUserService
{
    Task<User> GetUserByIdAsync(int userId);
    Task<IEnumerable<User>> GetAllUsersAsync();
}
