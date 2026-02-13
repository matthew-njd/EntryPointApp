using EntryPointApp.Api.Models.Dtos.Users;
using EntryPointApp.Api.Models.Enums;

namespace EntryPointApp.Api.Services.Admin
{
    public interface IAdminService
    {
        Task<UserListResult> GetAllUsersAsync();
        Task<UserResult> GetUserByIdAsync(int userId);
        Task<UserResult> UpdateUserRoleAsync(int userId, UserRole newRole);
        Task<UserResult> AssignManagerAsync(int userId, int managerId);
        Task<UserResult> RemoveManagerAsync(int userId);
        Task<UserResult> DeactivateUserAsync(int userId);
        Task<UserResult> ActivateUserAsync(int userId);
    }
}