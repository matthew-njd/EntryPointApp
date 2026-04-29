using EntryPointApp.Api.Models.Dtos.DailyLog;

namespace EntryPointApp.Api.Services.Receipt
{
    public interface IReceiptService
    {
        Task<ReceiptResult> UploadReceiptAsync(int dailyLogId, int weeklyLogId, IFormFile file, int requestingUserId);
        Task<ReceiptListResult> GetReceiptsAsync(int dailyLogId, int weeklyLogId, int requestingUserId);
        Task<ReceiptFileResult> DownloadReceiptAsync(int attachmentId, int dailyLogId, int weeklyLogId, int requestingUserId);
        Task<BaseDailyLogResult> DeleteReceiptAsync(int attachmentId, int dailyLogId, int weeklyLogId, int requestingUserId);
    }
}
