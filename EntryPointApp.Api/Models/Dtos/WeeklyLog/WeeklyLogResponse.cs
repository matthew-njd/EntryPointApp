using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Enums;

namespace EntryPointApp.Api.Models.Dtos.WeeklyLog
{
    public class WeeklyLogResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalCharges { get; set; }
        public TimesheetStatus Status { get; set; }
        public string? SalesRepComment { get; set; }
        public string? ManagerComment { get; set; }
    }

    public class WeeklyLogSummaryDto
    {
        public int TotalApproved { get; set; }
        public int TotalPending { get; set; }
        public int TotalDenied { get; set; }
        public int TotalDraft { get; set; }
    }

    public class WeeklyLogPagedResponse : PagedResult<WeeklyLogResponse>
    {
        public WeeklyLogSummaryDto Summary { get; set; } = new();
    }
}