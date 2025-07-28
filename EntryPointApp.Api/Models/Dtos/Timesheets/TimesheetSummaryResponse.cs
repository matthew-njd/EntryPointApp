namespace EntryPointApp.Api.Models.Dtos.Timesheets
{
    public class TimesheetSummaryResponse
    {
        public decimal TotalHours { get; set; }
        public decimal TotalMileage { get; set; }
        public decimal TotalExpenses { get; set; } // TollCharge + ParkingFee + OtherCharges
        public int TimesheetCount { get; set; }
    }
}