namespace EntryPointApp.Api.Models.Dtos.Timesheets
{
    public class TimesheetDto
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        public DateTime Date { get; set; }
        
        public decimal Hours { get; set; }
        
        public decimal Mileage { get; set; }
        
        public decimal TollCharge { get; set; }
        
        public decimal ParkingFee { get; set; }
        
        public decimal OtherCharges { get; set; }
        
        public string Comment { get; set; } = string.Empty;
    }
}