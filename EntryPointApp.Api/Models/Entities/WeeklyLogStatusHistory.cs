using System.ComponentModel.DataAnnotations;
using EntryPointApp.Api.Models.Enums;

namespace EntryPointApp.Api.Models.Entities
{
    public class WeeklyLogStatusHistory
    {
        public int Id { get; set; }

        public required int WeeklyLogId { get; set; }

        public required int ActorId { get; set; }

        public TimesheetStatus FromStatus { get; set; }

        public TimesheetStatus ToStatus { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public WeeklyLog WeeklyLog { get; set; } = null!;
        public User Actor { get; set; } = null!;
    }
}
