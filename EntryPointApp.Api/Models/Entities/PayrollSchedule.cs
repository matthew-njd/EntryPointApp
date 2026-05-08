namespace EntryPointApp.Api.Models.Entities
{
    public class PayrollSchedule
    {
        public int Id { get; set; }
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public DateOnly PayrollDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
