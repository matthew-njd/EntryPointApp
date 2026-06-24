namespace EntryPointApp.Api.Models.Entities
{
    public class UserRate
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal MileageRate { get; set; }
        public DateOnly EffectiveDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByAdminId { get; set; }

        public User User { get; set; } = null!;
        public User CreatedByAdmin { get; set; } = null!;
    }
}
