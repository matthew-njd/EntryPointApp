using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;
using EntryPointApp.Api.Models.Dtos.Weeklylog;

namespace EntryPointApp.Api.Services.Weeklylog
{
    public interface IWeeklylogService
    {
        Task<WeeklyLogListResult> GetWeeklyLogsAsync(int userId, PagedRequest request);

        Task<WeeklylogRequest> GetWeeklyLogByIdAsync(int id, int userId);
    }
}