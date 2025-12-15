using EntryPointApp.Api.Models.Common;
using EntryPointApp.Api.Models.Dtos.Authentication;
using EntryPointApp.Api.Services.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EntryPointApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthenticationService authService, ILogger<AuthController> logger) : ControllerBase
    {
        private readonly IAuthenticationService _authService = authService;
        private readonly ILogger<AuthController> _logger = logger;

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="request">Registration details</param>
        /// <returns>Registration result</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var result = await _authService.RegisterAsync(request);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<RegisterResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Registration Error",
                    Status = 500,
                    Detail = "An unexpected error occurred during registration",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Authenticate user and return JWT tokens
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Authentication result with tokens</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var result = await _authService.LoginAsync(request);

                if (!result.Success)
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for user: {Email}", request.Email);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Login Error",
                    Status = 500,
                    Detail = "An unexpected error occurred during login",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Refresh expired access token using refresh token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>New access and refresh tokens</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var result = await _authService.RefreshTokenAsync(request);

                if (!result.Success)
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token refresh");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Token Refresh Error",
                    Status = 500,
                    Detail = "An unexpected error occurred during token refresh",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Logout user and revoke refresh token
        /// </summary>
        /// <param name="request">Refresh token to revoke</param>
        /// <returns>Logout result</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var result = await _authService.LogoutAsync(request.RefreshToken);

                if (!result)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Logout failed",
                        Errors = ["Unable to revoke refresh token"]
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Logout successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during logout");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Logout Error",
                    Status = 500,
                    Detail = "An unexpected error occurred during logout",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Request password reset email
        /// </summary>
        /// <param name="request">Email address</param>
        /// <returns>Password reset request result</returns>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var result = await _authService.ForgotPasswordAsync(request);

                return Ok(new ApiResponse
                {
                    Success = result.Success,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during forgot password request");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Password Reset Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while processing your request",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Reset password using token
        /// </summary>
        /// <param name="request">Reset password details</param>
        /// <returns>Password reset result</returns>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var result = await _authService.ResetPasswordAsync(request);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password reset");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Password Reset Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while resetting your password",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Revoke all refresh tokens for the current user
        /// </summary>
        /// <returns>Revocation result</returns>
        [HttpPost("revoke-all")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> RevokeAllTokens()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user context",
                        Errors = ["Unable to identify user"]
                    });
                }

                var result = await _authService.RevokeAllTokensAsync(userId);

                if (!result)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Failed to revoke tokens",
                        Errors = ["Unable to revoke all refresh tokens"]
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "All refresh tokens revoked successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token revocation");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Token Revocation Error",
                    Status = 500,
                    Detail = "An unexpected error occurred during token revocation",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Validate if current JWT token is still valid
        /// </summary>
        /// <returns>Token validation result</returns>
        [HttpGet("validate")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public IActionResult ValidateToken()
        {
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Token is valid"
            });
        }
    }
}