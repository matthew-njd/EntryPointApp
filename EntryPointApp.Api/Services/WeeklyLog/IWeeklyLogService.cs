using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.WeeklyLog;

namespace EntryPointApp.Api.Services.WeeklyLog
{
    public interface IWeeklyLogService
    {
        Task<WeeklyLogListResult> GetWeeklyLogsAsync(int userId, PagedRequest request);

        Task<WeeklyLogResult> GetWeeklyLogByIdAsync(int id, int userId);

        Task<WeeklyLogResult> CreateWeeklyLogAsync(WeeklyLogRequest request, int userId);

        Task<WeeklyLogResult> UpdateWeeklyLogAsync(int id, WeeklyLogRequest request, int userId);

        Task<WeeklyLogResult> DeleteWeeklyLogAsync(int id, int userId);

        Task RecalculateWeeklyTotalsAsync(int weeklyLogId);

        Task<bool> WeeklyLogExistsAsync(int weeklyLogId, int userId);
    }
}