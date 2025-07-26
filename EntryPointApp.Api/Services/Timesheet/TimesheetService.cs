using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;

namespace EntryPointApp.Api.Services.Timesheet
{
    public class TimesheetService : ITimesheetService
    {
        public Task<TimesheetDto> CreateTimesheetAsync(TimesheetRequest request, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<List<TimesheetDto>> GetTimesheetsAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<TimesheetDto?> GetTimesheetByIdAsync(int id, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<TimesheetDto?> UpdateTimesheetAsync(int id, TimesheetRequest request, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteTimesheetAsync(int id, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<TimesheetDto>> GetTimesheetsAsync(int userId, PagedRequest request)
        {
            throw new NotImplementedException();
        }
    }
}