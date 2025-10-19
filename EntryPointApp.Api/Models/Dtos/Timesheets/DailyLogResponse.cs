namespace EntryPointApp.Api.Models.Dtos.Timesheets
{
    public class DailyLogResponse
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public decimal Hours { get; set; }
        public decimal Mileage { get; set; }
        public decimal TollCharge { get; set; }
        public decimal ParkingFee { get; set; }
        public decimal OtherCharges { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}