namespace EntryPointApp.Api.Models.Dtos.PayrollSchedule
{
    public class PayrollScheduleResponse
    {
        public int Id { get; set; }
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public DateOnly PayrollDate { get; set; }
    }

    public class PayrollScheduleRequest
    {
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public DateOnly PayrollDate { get; set; }
    }

    public class PayrollScheduleLookupResponse
    {
        public DateOnly? PayrollDate { get; set; }
    }

    public class PayrollScheduleImportStats
    {
        public int Imported { get; set; }
    }

    public class BasePayrollScheduleResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
    }

    public class PayrollScheduleResult : BasePayrollScheduleResult
    {
        public PayrollScheduleResponse? Data { get; set; }
    }

    public class PayrollScheduleListResult : BasePayrollScheduleResult
    {
        public List<PayrollScheduleResponse>? Data { get; set; }
    }

    public class PayrollScheduleLookupResult : BasePayrollScheduleResult
    {
        public PayrollScheduleLookupResponse? Data { get; set; }
    }

    public class PayrollScheduleImportResult : BasePayrollScheduleResult
    {
        public PayrollScheduleImportStats? Data { get; set; }
    }
}
