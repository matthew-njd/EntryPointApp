using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;

namespace EntryPointApp.Api.Services.Timesheet
{
    public interface ITimesheetService
    {
        Task<TimesheetListResult> GetTimesheetsAsync(int userId, PagedRequest request);

        Task<TimesheetResponse?> GetTimesheetByIdAsync(int id, int userId);
        
        Task<TimesheetResponse> CreateTimesheetAsync(TimesheetRequest request, int userId);
        
        Task<TimesheetResponse?> UpdateTimesheetAsync(int id, TimesheetRequest request, int userId);
        
        Task<bool> DeleteTimesheetAsync(int id, int userId);
    }
}