using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;
using EntryPointApp.Api.Models.Dtos.Weeklylog;

namespace EntryPointApp.Api.Services.Weeklylog
{
    public class WeeklylogService(ApplicationDbContext context, ILogger<WeeklylogService> logger) : IWeeklylogService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<WeeklylogService> _logger = logger;

        public Task<WeeklylogRequest> GetWeeklyLogByIdAsync(int id, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<WeeklyLogListResult> GetWeeklyLogsAsync(int userId, PagedRequest request)
        {
            throw new NotImplementedException();
        }
    }
}