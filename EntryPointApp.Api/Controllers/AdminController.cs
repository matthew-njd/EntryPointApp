using System.Security.Claims;
using EntryPointApp.Api.Models.Common;
using EntryPointApp.Api.Models.Dtos.Users;
using EntryPointApp.Api.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntryPointApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController(IAdminService adminService, IUserRateService userRateService, ILogger<AdminController> logger) : ControllerBase
    {
        private readonly IAdminService _adminService = adminService;
        private readonly IUserRateService _userRateService = userRateService;
        private readonly ILogger<AdminController> _logger = logger;

        /// <summary>
        /// Get all users in the system
        /// </summary>
        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiResponse<List<UserDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var result = await _adminService.GetAllUsersAsync();

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Admin {AdminId} retrieved all users", adminId);

                return Ok(new ApiResponse<List<UserDto>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving users");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "User Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving users",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get a specific user by ID
        /// </summary>
        [HttpGet("users/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetUserById([FromRoute] int userId)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["User ID must be greater than 0"]
                    });
                }

                var result = await _adminService.GetUserByIdAsync(userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Admin {AdminId} retrieved user {UserId}", adminId, userId);

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving user {UserId}", userId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "User Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving the user",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Update a user's role (User/Manager/Admin)
        /// </summary>
        [HttpPut("users/{userId}/role")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> UpdateUserRole([FromRoute] int userId, [FromBody] UpdateUserRoleRequest request)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["User ID must be greater than 0"]
                    });
                }

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

                var result = await _adminService.UpdateUserRoleAsync(userId, request.Role);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Admin {AdminId} updated role for user {UserId} to {Role}", adminId, userId, request.Role);

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating role for user {UserId}", userId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Role Update Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while updating the user role",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Assign a manager to a user
        /// </summary>
        [HttpPut("users/{userId}/manager")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> AssignManager([FromRoute] int userId, [FromBody] AssignManagerRequest request)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["User ID must be greater than 0"]
                    });
                }

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

                var result = await _adminService.AssignManagerAsync(userId, request.ManagerId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Admin {AdminId} assigned manager {ManagerId} to user {UserId}", adminId, request.ManagerId, userId);

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error assigning manager to user {UserId}", userId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Manager Assignment Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while assigning the manager",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Remove manager assignment from a user
        /// </summary>
        [HttpDelete("users/{userId}/manager")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> RemoveManager([FromRoute] int userId)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["User ID must be greater than 0"]
                    });
                }

                var result = await _adminService.RemoveManagerAsync(userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Admin {AdminId} removed manager from user {UserId}", adminId, userId);

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error removing manager from user {UserId}", userId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Manager Removal Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while removing the manager",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Deactivate a user account
        /// </summary>
        [HttpPut("users/{userId}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> DeactivateUser([FromRoute] int userId)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["User ID must be greater than 0"]
                    });
                }

                var result = await _adminService.DeactivateUserAsync(userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Admin {AdminId} deactivated user {UserId}", adminId, userId);

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deactivating user {UserId}", userId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "User Deactivation Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while deactivating the user",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get full rate history for a user
        /// </summary>
        [HttpGet("users/{userId}/rates")]
        [ProducesResponseType(typeof(ApiResponse<List<UserRateDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetUserRates([FromRoute] int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["User ID must be greater than 0"]
                    });
                }

                var result = await _userRateService.GetRatesForUserAsync(userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<List<UserRateDto>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving rates for user {UserId}", userId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Rate Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving user rates",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get the current effective rate for a user
        /// </summary>
        [HttpGet("users/{userId}/rates/current")]
        [ProducesResponseType(typeof(ApiResponse<UserRateDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetCurrentUserRate([FromRoute] int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["User ID must be greater than 0"]
                    });
                }

                var result = await _userRateService.GetCurrentRateAsync(userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<UserRateDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving current rate for user {UserId}", userId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Rate Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving the current user rate",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Set a new rate entry for a user
        /// </summary>
        [HttpPost("users/{userId}/rates")]
        [ProducesResponseType(typeof(ApiResponse<UserRateDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> SetUserRate([FromRoute] int userId, [FromBody] SetUserRateRequest request)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["User ID must be greater than 0"]
                    });
                }

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

                if (!int.TryParse(adminId, out var parsedAdminId))
                {
                    return Unauthorized();
                }

                var result = await _userRateService.SetRateAsync(userId, request, parsedAdminId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Admin {AdminId} set rate for user {UserId}", adminId, userId);

                return StatusCode(201, new ApiResponse<UserRateDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error setting rate for user {UserId}", userId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Rate Set Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while setting the user rate",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Activate a user account
        /// </summary>
        [HttpPut("users/{userId}/activate")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> ActivateUser([FromRoute] int userId)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["User ID must be greater than 0"]
                    });
                }

                var result = await _adminService.ActivateUserAsync(userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Admin {AdminId} activated user {UserId}", adminId, userId);

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error activating user {UserId}", userId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "User Activation Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while activating the user",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get all timesheets for a specific user
        /// </summary>
        [HttpGet("users/{userId}/timesheets")]
        [ProducesResponseType(typeof(ApiResponse<List<AdminTimesheetResponse>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetUserTimesheets(int userId)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var result = await _adminService.GetUserTimesheetsAsync(userId);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Admin {AdminId} retrieved timesheets for user {UserId}", adminId, userId);

                return Ok(new ApiResponse<List<AdminTimesheetResponse>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving timesheets for user {UserId}", userId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Timesheet Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving timesheets",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get a specific timesheet detail for a user
        /// </summary>
        [HttpGet("users/{userId}/timesheets/{timesheetId}")]
        [ProducesResponseType(typeof(ApiResponse<AdminTimesheetDetailResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetUserTimesheetDetail(int userId, int timesheetId)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var result = await _adminService.GetUserTimesheetDetailAsync(timesheetId, userId);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Admin {AdminId} retrieved timesheet {TimesheetId} for user {UserId}", adminId, timesheetId, userId);

                return Ok(new ApiResponse<AdminTimesheetDetailResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving timesheet {TimesheetId} for user {UserId}", timesheetId, userId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Timesheet Detail Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving the timesheet detail",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }
    }
}