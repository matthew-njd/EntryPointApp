using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Configuration;
using EntryPointApp.Api.Models.Dtos.Authentication;
using EntryPointApp.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EntryPointApp.Api.Services.Authentication
{
    public class AuthenticationService(
        ApplicationDbContext context,
        IJwtService jwtService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthenticationService> logger) : IAuthenticationService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IJwtService _jwtService = jwtService;
        private readonly JwtSettings _jwtSettings = jwtSettings.Value;
        private readonly ILogger<AuthenticationService> _logger = logger;

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var existingUser = await GetUserByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "User already exists",
                        Errors = ["A user with this email already exists"]
                    };
                }

                if (request.ManagerId.HasValue)
                {
                    var manager = await GetUserByIdAsync(request.ManagerId.Value);
                    if (manager == null)
                    {
                        return new AuthResult
                        {
                            Success = false,
                            Message = "Invalid manager",
                            Errors = ["The specified manager does not exist"]
                        };
                    }
                }

                var user = new User
                {
                    Email = request.Email.ToLowerInvariant(),
                    PasswordHash = HashPassword(request.Password),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Role = request.Role,
                    ManagerId = request.ManagerId ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var accessToken = _jwtService.GenerateToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    User = user,
                    ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                var response = new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
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
                    Message = "User registered successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user: {Email}", request.Email);
                return new AuthResult
                {
                    Success = false,
                    Message = "An error occurred during registration",
                    Errors = ["Internal server error"]
                };
            }
        }
        
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
                        Errors = ["Invalid credentials"]
                    };
                }

                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = ["Invalid credentials"]
                    };
                }

                var accessToken = _jwtService.GenerateToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    User = user,
                    ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                var response = new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
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
                    Errors = ["Internal server error"]
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
                        Errors = ["Invalid refresh token"]
                    };
                }

                if (!refreshToken.User.IsActive)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "User account is not active",
                        Errors = ["Account not active"]
                    };
                }

                var newAccessToken = _jwtService.GenerateToken(refreshToken.User);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.ReplacedBy = newRefreshToken;

                var newRefreshTokenEntity = new RefreshToken
                {
                    Token = newRefreshToken,
                    UserId = refreshToken.UserId,
                    User = refreshToken.User,
                    ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(newRefreshTokenEntity);
                await _context.SaveChangesAsync();

                var response = new LoginResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
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
                    Errors = ["Internal server error"]
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
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
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