using EntryPointApp.Api.Models.Dtos.Common;

namespace EntryPointApp.Api.Models.Dtos.WeeklyLog
{
    public class BaseWeeklylogResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
    }

    public class WeeklyLogResult : BaseWeeklylogResult
    {
        public WeeklyLogResponse? Data { get; set; }
    }

    public class WeeklyLogListResult : BaseWeeklylogResult
    {
        public PagedResult<WeeklyLogResponse>? Data { get; set; }
    }
}