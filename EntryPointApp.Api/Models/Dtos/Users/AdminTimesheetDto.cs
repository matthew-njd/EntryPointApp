using EntryPointApp.Api.Models.Dtos.DailyLog;

namespace EntryPointApp.Api.Models.Dtos.Users
{
    public class AdminTimesheetResponse
    {
        public int Id { get; set; }
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalCharges { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ManagerComment { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AdminTimesheetDetailResponse : AdminTimesheetResponse
    {
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public List<AdminDailyLogResponse> DailyLogs { get; set; } = [];
    }

    public class AdminDailyLogResponse
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly TimeIn { get; set; }
        public TimeOnly TimeOut { get; set; }
        public decimal Hours => (decimal)(TimeOut - TimeIn).TotalHours;
        public decimal Mileage { get; set; }
        public decimal TollCharge { get; set; }
        public decimal ParkingFee { get; set; }
        public decimal OtherCharges { get; set; }
        public string Comment { get; set; } = string.Empty;
        public List<ReceiptResponse> Receipts { get; set; } = [];
    }

    public class AdminTimesheetListResult : BaseAdminResult
    {
        public List<AdminTimesheetResponse>? Data { get; set; }
    }

    public class AdminTimesheetDetailResult : BaseAdminResult
    {
        public AdminTimesheetDetailResponse? Data { get; set; }
    }
}
