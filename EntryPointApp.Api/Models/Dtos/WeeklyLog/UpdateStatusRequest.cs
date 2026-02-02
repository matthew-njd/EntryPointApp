using EntryPointApp.Api.Models.Enums;

namespace EntryPointApp.Api.Models.Dtos.WeeklyLog
{
    public class UpdateStatusRequest
    {
        public TimesheetStatus Status { get; set; }
    }
}