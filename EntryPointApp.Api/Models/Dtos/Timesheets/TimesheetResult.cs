using EntryPointApp.Api.Models.Dtos.Common;

namespace EntryPointApp.Api.Models.Dtos.Timesheets
{
    public class BaseTimesheetResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
    }

    public class TimesheetResult : BaseTimesheetResult
    {
        public TimesheetResponse? Data { get; set; }
    }

    public class TimesheetListResult : BaseTimesheetResult
    {
        public PagedResult<TimesheetResponse>? Data { get; set; }
    }
}