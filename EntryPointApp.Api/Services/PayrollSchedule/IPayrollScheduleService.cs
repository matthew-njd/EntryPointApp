using EntryPointApp.Api.Models.Dtos.PayrollSchedule;

namespace EntryPointApp.Api.Services.PayrollSchedule
{
    public interface IPayrollScheduleService
    {
        Task<PayrollScheduleListResult> GetAllAsync();
        Task<PayrollScheduleResult> CreateAsync(DateOnly dateFrom, DateOnly dateTo, DateOnly payrollDate);
        Task<PayrollScheduleResult> UpdateAsync(int id, DateOnly dateFrom, DateOnly dateTo, DateOnly payrollDate);
        Task<BasePayrollScheduleResult> DeleteAsync(int id);
        Task<PayrollScheduleImportResult> BulkImportAsync(List<PayrollScheduleImportItem> entries, bool replace);
        Task<PayrollScheduleLookupResult> LookupAsync(DateOnly timesheetDateFrom);
    }

    public record PayrollScheduleImportItem(DateOnly DateFrom, DateOnly DateTo, DateOnly PayrollDate);
}
