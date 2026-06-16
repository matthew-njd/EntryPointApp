using EntryPointApp.Api.Models.Dtos.Admin;

namespace EntryPointApp.Api.Services.Admin
{
    public interface IAdminSummaryService
    {
        Task<PayrollSummaryResult> GetPayrollSummaryAsync(DateOnly dateFrom, DateOnly dateTo);
    }
}
