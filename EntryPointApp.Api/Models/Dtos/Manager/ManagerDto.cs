using System.ComponentModel.DataAnnotations;
using EntryPointApp.Api.Models.Dtos.Common;

namespace EntryPointApp.Api.Models.Dtos.Manager
{
    public class TeamTimesheetResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalCharges { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ManagerComment { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TeamTimesheetDetailResponse : TeamTimesheetResponse
    {
        public List<TeamDailyLogResponse> DailyLogs { get; set; } = [];
    }

    public class TeamDailyLogResponse
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public decimal Hours { get; set; }
        public decimal Mileage { get; set; }
        public decimal TollCharge { get; set; }
        public decimal ParkingFee { get; set; }
        public decimal OtherCharges { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

    public class DenyTimesheetRequest
    {
        [Required(ErrorMessage = "A reason is required when denying a timesheet.")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public required string Reason { get; set; }
    }

    public class ApproveTimesheetRequest
    {
        [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
        public string? Comment { get; set; }
    }

    public class BaseManagerResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
    }

    public class TeamTimesheetResult : BaseManagerResult
    {
        public TeamTimesheetResponse? Data { get; set; }
    }

    public class TeamTimesheetDetailResult : BaseManagerResult
    {
        public TeamTimesheetDetailResponse? Data { get; set; }
    }

    public class TeamTimesheetListResult : BaseManagerResult
    {
        public List<TeamTimesheetResponse>? Data { get; set; }
    }

    public class TeamTimesheetPagedResult : BaseManagerResult
    {
        public PagedResult<TeamTimesheetResponse>? Data { get; set; }
    }
}