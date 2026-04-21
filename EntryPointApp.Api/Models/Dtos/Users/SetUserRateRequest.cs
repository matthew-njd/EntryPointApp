using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Dtos.Users
{
    public class SetUserRateRequest
    {
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Hourly rate must be non-negative")]
        public decimal HourlyRate { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Mileage rate must be non-negative")]
        public decimal MileageRate { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }
    }
}
