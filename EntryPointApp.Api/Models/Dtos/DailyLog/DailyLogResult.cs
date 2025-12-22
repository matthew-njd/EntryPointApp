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
}