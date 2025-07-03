using System.Security.Claims;
using EntryPointApp.Api.Models.Entities;

namespace EntryPointApp.Api.Services.Authentication
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        bool ValidateToken(string token);
    }
}