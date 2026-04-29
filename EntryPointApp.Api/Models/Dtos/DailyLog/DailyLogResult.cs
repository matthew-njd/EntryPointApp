namespace EntryPointApp.Api.Models.Dtos.DailyLog
{
    public class BaseDailyLogResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
    }

    public class DailyLogResult : BaseDailyLogResult
    {
        public DailyLogResponse? Data { get; set; }
    }

    public class DailyLogListResult : BaseDailyLogResult
    {
        public List<DailyLogResponse>? Data { get; set; }
    }

    public class ReceiptResult : BaseDailyLogResult
    {
        public ReceiptResponse? Data { get; set; }
    }

    public class ReceiptListResult : BaseDailyLogResult
    {
        public List<ReceiptResponse>? Data { get; set; }
    }

    public class ReceiptFileResult : BaseDailyLogResult
    {
        public Stream? FileStream { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}