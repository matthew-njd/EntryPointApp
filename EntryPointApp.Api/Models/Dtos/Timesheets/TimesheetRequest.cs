using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Dtos.Timesheets
{
    public class TimesheetRequest
    {
        [Required(ErrorMessage = "A date is required.")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "The number of hours is required.")]
        public decimal Hours { get; set; }

        [Required(ErrorMessage = "The mileage is required.")]
        public decimal Mileage { get; set; }

        public decimal TollCharge { get; set; }

        public decimal ParkingFee { get; set; }

        public decimal OtherCharges { get; set; }
        
        public string Comment { get; set; } = string.Empty;
    }
}