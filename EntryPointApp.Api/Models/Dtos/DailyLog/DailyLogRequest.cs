using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Dtos.DailyLog
{
    public class DailyLogRequest
    {
        [Required(ErrorMessage = "A date is required.")]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "A time in is required.")]
        public TimeOnly TimeIn { get; set; }

        [Required(ErrorMessage = "A time out is required.")]
        public TimeOnly TimeOut { get; set; }

        [Required(ErrorMessage = "The mileage is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Mileage must be a positive number.")]
        public decimal Mileage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Toll charge must be a positive number.")]
        public decimal TollCharge { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Parking fee must be a positive number.")]
        public decimal ParkingFee { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Other charges must be a positive number.")]
        public decimal OtherCharges { get; set; }
        
        [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
        public string? Comment { get; set; }
    }
}