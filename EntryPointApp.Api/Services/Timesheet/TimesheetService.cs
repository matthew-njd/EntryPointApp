using EntryPointApp.Api.Models.Dtos.Timesheets;

namespace EntryPointApp.Api.Services.Timesheet
{
    public class TimesheetService : ITimesheetService
    {
        public Task<TimesheetDto> GetTimesheetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<TimesheetDto> GetTimesheetsAsync()
        {
            throw new NotImplementedException();
        }
    }
}