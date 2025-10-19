using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Entities
{
    public class WeeklyLog
    {
        public int Id { get; set; }

        public required int UserId { get; set; }

        public required DateOnly DateFrom { get; set; }
        
        public required DateOnly DateTo { get; set; }

        public decimal TotalHours { get; set; }

        public decimal TollCharges { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<DailyLog> DailyLogs { get; set; } = [];
    }
}