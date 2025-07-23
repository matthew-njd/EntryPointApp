using EntryPointApp.Api.Models.Dtos.Timesheets;

namespace EntryPointApp.Api.Services.Timesheet
{
    public interface ITimesheetService
    {
        public Task<TimesheetDto> GetTimesheetsAsync();
        public Task<TimesheetDto> GetTimesheetByIdAsync(int id);

    }
}