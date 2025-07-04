using EntryPointApp.Api.Models.Dtos.Authentication;
using EntryPointApp.Api.Models.Entities;

namespace EntryPointApp.Api.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<AuthResult> LoginAsync(LoginRequest request);
        Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> LogoutAsync(string refreshToken);
        Task<bool> RevokeAllTokensAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        bool VerifyPassword(string password, string hashedPassword);
        string HashPassword(string password);
    }
}