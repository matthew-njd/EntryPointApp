using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;

namespace EntryPointApp.Api.Services.Timesheet
{
    public interface ITimesheetService
    {
        Task<PagedResult<TimesheetDto>> GetTimesheetsAsync(int userId, PagedRequest request);

        Task<TimesheetDto?> GetTimesheetByIdAsync(int id, int userId);
        
        Task<TimesheetDto> CreateTimesheetAsync(TimesheetRequest request, int userId);
        
        Task<TimesheetDto?> UpdateTimesheetAsync(int id, TimesheetRequest request, int userId);
        
        Task<bool> DeleteTimesheetAsync(int id, int userId);
    }
}