using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Authentication;
using EntryPointApp.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.Authentication
{
    public class AuthenticationService(
        ApplicationDbContext context,
        IJwtService jwtService,
        ILogger<AuthenticationService> logger) : IAuthenticationService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IJwtService _jwtService = jwtService;
        private readonly ILogger<AuthenticationService> _logger = logger;

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await GetUserByEmailAsync(request.Email);

                if (user == null || !user.IsActive)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "Invalid credentials" }
                    };
                }

                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "Invalid credentials" }
                    };
                }

                // Generate tokens
                var accessToken = _jwtService.GenerateToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Save refresh token to database
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    User = user,
                    ExpiryDate = DateTime.UtcNow.AddDays(7), // Should come from configuration
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                var response = new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30), // Should come from JWT settings
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = user.Role.ToString(),
                        ManagerId = user.ManagerId == 0 ? null : user.ManagerId
                    }
                };

                return new AuthResult
                {
                    Success = true,
                    Message = "Login successful",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Email}", request.Email);
                return new AuthResult
                {
                    Success = false,
                    Message = "An error occurred during login",
                    Errors = new List<string> { "Internal server error" }
                };
            }
        }

        public async Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request)
        {
            try
            {
                var refreshToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

                if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiryDate <= DateTime.UtcNow)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid or expired refresh token",
                        Errors = new List<string> { "Invalid refresh token" }
                    };
                }

                if (!refreshToken.User.IsActive)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "User account is not active",
                        Errors = new List<string> { "Account not active" }
                    };
                }

                // Generate new tokens
                var newAccessToken = _jwtService.GenerateToken(refreshToken.User);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                // Revoke old refresh token
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.ReplacedBy = newRefreshToken;

                // Create new refresh token
                var newRefreshTokenEntity = new RefreshToken
                {
                    Token = newRefreshToken,
                    UserId = refreshToken.UserId,
                    User = refreshToken.User,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(newRefreshTokenEntity);
                await _context.SaveChangesAsync();

                var response = new LoginResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                    User = new UserInfo
                    {
                        Id = refreshToken.User.Id,
                        Email = refreshToken.User.Email,
                        FirstName = refreshToken.User.FirstName,
                        LastName = refreshToken.User.LastName,
                        Role = refreshToken.User.Role.ToString(),
                        ManagerId = refreshToken.User.ManagerId == 0 ? null : refreshToken.User.ManagerId
                    }
                };

                return new AuthResult
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return new AuthResult
                {
                    Success = false,
                    Message = "An error occurred during token refresh",
                    Errors = new List<string> { "Internal server error" }
                };
            }
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            try
            {
                var token = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (token != null)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return false;
            }
        }

        public async Task<bool> RevokeAllTokensAsync(int userId)
        {
            try
            {
                var tokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all tokens for user: {UserId}", userId);
                return false;
            }
        }
        
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}