using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;

namespace EntryPointApp.Api.Models.Dtos.Weeklylog
{
    public class BaseWeeklylogResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
    }

    public class WeeklyLogResult : BaseWeeklylogResult
    {
        public WeeklylogResponse? Data { get; set; }
    }

    public class WeeklyLogListResult : BaseWeeklylogResult
    {
        public PagedResult<WeeklylogResponse>? Data { get; set; }
    }
}