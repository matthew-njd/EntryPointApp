using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;

namespace EntryPointApp.Api.Services.Timesheet
{
    public class TimesheetService(ApplicationDbContext context, ILogger<TimesheetService> logger) : ITimesheetService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<TimesheetService> _logger = logger;

        public Task<PagedResult<TimesheetDto>> GetTimesheetsAsync(int userId, PagedRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<TimesheetDto?> GetTimesheetByIdAsync(int id, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<TimesheetDto> CreateTimesheetAsync(TimesheetRequest request, int userId)
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
    }
}