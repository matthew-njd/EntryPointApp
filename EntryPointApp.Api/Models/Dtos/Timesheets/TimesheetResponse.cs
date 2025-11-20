using EntryPointApp.Api.Models.Dtos.DailyLog;

namespace EntryPointApp.Api.Models.Dtos.Timesheets
{
    public class TimesheetResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalCharges { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<DailyLogResponse> DailyLogs { get; set; } = [];
    }
}