using EntryPointApp.Api.Models.Dtos.Common;

namespace EntryPointApp.Api.Models.Dtos.WeeklyLog
{
    public class BaseWeeklyLogResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
    }

    public class WeeklyLogResult : BaseWeeklyLogResult
    {
        public WeeklyLogResponse? Data { get; set; }
    }

    public class WeeklyLogListResult : BaseWeeklyLogResult
    {
        public PagedResult<WeeklyLogResponse>? Data { get; set; }
    }
}