using EntryPointApp.Api.Models.Dtos.Authentication;
using EntryPointApp.Api.Models.Entities;


namespace EntryPointApp.Api.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<RegisterAuthResult> RegisterAsync(RegisterRequest request);
        Task<LoginAuthResult> LoginAsync(LoginRequest request);
        Task<RefreshTokenAuthResult> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> LogoutAsync(string refreshToken);
        Task<bool> RevokeAllTokensAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        bool VerifyPassword(string password, string hashedPassword);
        string HashPassword(string password);
        Task<ForgotPasswordAuthResult> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ResetPasswordAuthResult> ResetPasswordAsync(ResetPasswordRequest request);
    }
}