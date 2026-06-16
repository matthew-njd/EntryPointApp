using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Dtos.ApprovedEmails
{
    public class AddApprovedEmailRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;
    }
}
