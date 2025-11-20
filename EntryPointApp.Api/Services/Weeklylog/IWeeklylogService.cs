using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.WeeklyLog;

namespace EntryPointApp.Api.Services.WeeklyLog
{
    public interface IWeeklyLogService
    {
        Task<WeeklyLogListResult> GetWeeklyLogsAsync(int userId, PagedRequest request);

        Task<WeeklyLogResult> GetWeeklyLogByIdAsync(int id, int userId);
    }
}