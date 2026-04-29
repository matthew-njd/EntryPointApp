using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Entities
{
    public class DailyLogAttachment
    {
        public int Id { get; set; }

        public required int DailyLogId { get; set; }

        [MaxLength(260)]
        public required string FileName { get; set; }

        [MaxLength(260)]
        public required string OriginalFileName { get; set; }

        [MaxLength(100)]
        public required string ContentType { get; set; }

        public long FileSizeBytes { get; set; }

        public DateTime UploadedAt { get; set; }

        public DailyLog DailyLog { get; set; } = null!;
    }
}
