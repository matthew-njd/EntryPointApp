using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Dtos.Authentication
{
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public required string Email { get; set; }
    }
}