using EntryPointApp.Api.Models.Dtos.Manager;

namespace EntryPointApp.Api.Services.SalesRep
{
    public interface ISalesRepService
    {
        Task<TeamTimesheetPagedResult> GetTeamTimesheetsAsync(int salesRepId, int page, int pageSize, string? statusFilter, string? search);
        Task<TeamTimesheetListResult> GetPendingTimesheetsAsync(int salesRepId);
        Task<TeamTimesheetDetailResult> GetTimesheetDetailAsync(int weeklyLogId, int salesRepId);
        Task<TeamTimesheetResult> ApproveTimesheetAsync(int weeklyLogId, int salesRepId, ApproveTimesheetRequest request);
        Task<TeamTimesheetResult> DenyTimesheetAsync(int weeklyLogId, int salesRepId, DenyTimesheetRequest request);
    }
}
