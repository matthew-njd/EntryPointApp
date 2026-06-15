namespace EntryPointApp.Api.Services.Excel
{
    public interface IExcelService
    {
        Task<byte[]> GenerateTimesheetExcelAsync(int weeklyLogId, decimal hourlyRate, decimal mileageRate);
    }
}