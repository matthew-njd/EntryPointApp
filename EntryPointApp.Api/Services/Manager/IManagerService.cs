using EntryPointApp.Api.Models.Dtos.Manager;

namespace EntryPointApp.Api.Services.Manager
{
    public interface IManagerService
    {
        Task<TeamTimesheetPagedResult> GetTeamTimesheetsAsync(int managerId, int page, int pageSize, string? statusFilter);
        Task<TeamTimesheetListResult> GetPendingTimesheetsAsync(int managerId);
        Task<TeamTimesheetDetailResult> GetTimesheetDetailAsync(int weeklyLogId, int managerId);
        Task<TeamTimesheetResult> ApproveTimesheetAsync(int weeklyLogId, int managerId, ApproveTimesheetRequest request);
        Task<TeamTimesheetResult> DenyTimesheetAsync(int weeklyLogId, int managerId, DenyTimesheetRequest request);
    }
}