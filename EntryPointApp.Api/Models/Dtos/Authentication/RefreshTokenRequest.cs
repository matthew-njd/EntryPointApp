using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Dtos.Authentication
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}