namespace EntryPointApp.Api.Models.Dtos.DailyLog
{
    public class ReceiptResponse
    {
        public int Id { get; set; }
        public int DailyLogId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
