using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Dtos.WeeklyLog
{
    public class WeeklyLogRequest
    {
        [Required(ErrorMessage = "Start date is required.")]
        public DateOnly DateFrom { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateOnly DateTo { get; set; }

        [MaxLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string? Status { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateTo < DateFrom)
            {
                yield return new ValidationResult(
                    "End date must be after or equal to start date.",
                    [nameof(DateTo)]
                );
            }

            var daysDifference = DateTo.DayNumber - DateFrom.DayNumber + 1;
            if (daysDifference != 7)
            {
                yield return new ValidationResult(
                    "Timesheet must cover exactly 7 days (one week).",
                    [nameof(DateFrom), nameof(DateTo)]
                );
            }
        }
    }
}