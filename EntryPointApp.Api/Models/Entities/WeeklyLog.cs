using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Entities
{
    public class WeeklyLog
    {
        public int Id { get; set; }

        public required int UserId { get; set; }

        public required DateOnly Date { get; set; }

        public decimal Hours { get; set; }

        public decimal Mileage { get; set; }

        public decimal TollCharge { get; set; }

        public decimal ParkingFee { get; set; }

        public decimal OtherCharges { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        [MaxLength(500)]
        public string Comment { get; set; } = string.Empty;

        public User User { get; set; } = null!;
    }
}