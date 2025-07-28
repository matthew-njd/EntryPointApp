using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;

namespace EntryPointApp.Api.Services.Timesheet
{
    public interface ITimesheetService
    {
        Task<TimesheetListResult> GetTimesheetsAsync(int userId, PagedRequest request);

        Task<TimesheetResult> GetTimesheetByIdAsync(int id, int userId);
        
        Task<TimesheetResult> CreateTimesheetAsync(TimesheetRequest request, int userId);
        
        Task<TimesheetResult> UpdateTimesheetAsync(int id, TimesheetRequest request, int userId);
        
        Task<TimesheetResult> DeleteTimesheetAsync(int id, int userId);
    }
}