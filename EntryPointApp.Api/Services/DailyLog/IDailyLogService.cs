using EntryPointApp.Api.Models.Dtos.DailyLog;

namespace EntryPointApp.Api.Services.DailyLog
{
    public interface IDailyLogService
    {
        Task<DailyLogListResult> GetDailyLogsByWeeklyLogIdAsync(int weeklyLogId, int userId);

        Task<DailyLogResult> GetDailyLogByIdAsync(int id, int weeklyLogId, int userId);

        Task<DailyLogResult> CreateDailyLogAsync(int weeklyLogId, DailyLogRequest request, int userId);

        Task<DailyLogResult> UpdateDailyLogAsync(int id, int weeklyLogId, DailyLogRequest request, int userId);

        Task<DailyLogResult> DeleteDailyLogAsync(int id, int weeklyLogId, int userId);
    }
}