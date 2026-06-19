using EntryPointApp.Api.Models.Dtos.Users;
using EntryPointApp.Api.Models.Enums;

namespace EntryPointApp.Api.Services.Admin
{
    public interface IAdminService
    {
        Task<UserPagedResult> GetAllUsersAsync(int page, int pageSize, string? roleFilter, string? statusFilter, string? search);
        Task<UserResult> GetUserByIdAsync(int userId);
        Task<UserResult> UpdateUserRoleAsync(int userId, UserRole newRole);
        Task<UserResult> UpdateEmployeeTypeAsync(int userId, EmployeeType? employeeType);
        Task<UserResult> AssignManagerAsync(int userId, int managerId);
        Task<UserResult> RemoveManagerAsync(int userId);
        Task<UserResult> AssignSalesRepAsync(int userId, int salesRepId);
        Task<UserResult> RemoveSalesRepAsync(int userId);
        Task<UserResult> DeactivateUserAsync(int userId);
        Task<UserResult> ActivateUserAsync(int userId);
        Task<AdminTimesheetListResult> GetUserTimesheetsAsync(int userId);
        Task<AdminTimesheetDetailResult> GetUserTimesheetDetailAsync(int weeklyLogId, int userId);
    }
}