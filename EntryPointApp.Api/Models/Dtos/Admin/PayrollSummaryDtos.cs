namespace EntryPointApp.Api.Models.Dtos.Admin
{
    public class PayrollSummaryItemDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string EmployeeType { get; set; } = "";
        public decimal HourlyRate { get; set; }
        public decimal MileageRate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalMileage { get; set; }
        public decimal TotalTollCharges { get; set; }
        public decimal TotalParkingFees { get; set; }
        public decimal TotalOtherCharges { get; set; }
        public decimal GrossPay { get; set; }
        public decimal MileageReimbursement { get; set; }
    }

    public class PayrollSummaryResponse
    {
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public List<PayrollSummaryItemDto> Items { get; set; } = [];
    }

    public class BasePayrollSummaryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<string> Errors { get; set; } = [];
    }

    public class PayrollSummaryResult : BasePayrollSummaryResult
    {
        public PayrollSummaryResponse? Data { get; set; }
    }
}
