using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Dtos.Authentication
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Reset token is required")]
        public required string Token { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
        public required string NewPassword { get; set; }

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public required string ConfirmPassword { get; set; }
    }
}