using EntryPointApp.Api.Models.Dtos.Users;

namespace EntryPointApp.Api.Models.Dtos.Authentication
{
    public class RegisterResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserResponse User { get; set; } = null!;
        public string WelcomeMessage { get; set; } = "Welcome! Your account has been created successfully.";
    }
}