using System.Security.Claims;
using EntryPointApp.Api.Models.Common;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.WeeklyLog;
using EntryPointApp.Api.Services.WeeklyLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntryPointApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeeklyLogController(IWeeklyLogService weeklyLogService, ILogger<WeeklyLogController> logger) : ControllerBase
    {
        private readonly IWeeklyLogService _weeklyLogService = weeklyLogService;
        private readonly ILogger<WeeklyLogController> _logger = logger;

        /// <summary>
        /// Get all weeklylogs with pagination and optional date filtering
        /// </summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<WeeklyLogResponse>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetWeeklyLogs([FromQuery] PagedRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user",
                        Errors = ["Unable to identify user"]
                    });
                }

                if (request.Page < 1)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Page number must be 1 or greater"]
                    });
                }

                if (request.PageSize < 1 || request.PageSize > 100)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Page size must be between 1 and 100"]
                    });
                }

                if (!request.StartDate.HasValue && !request.EndDate.HasValue)
                {
                    request.EndDate = DateOnly.FromDateTime(DateTime.Now);
                    request.StartDate = request.EndDate.Value.AddMonths(-12);

                    _logger.LogInformation("Applied default date range for user {UserId}: {StartDate} to {EndDate}",
                        userId, request.StartDate, request.EndDate);
                }

                if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate > request.EndDate)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Start date cannot be after end date"]
                    });
                }

                if (request.StartDate.HasValue && request.EndDate.HasValue)
                {
                    var daysDifference = request.EndDate.Value.DayNumber - request.StartDate.Value.DayNumber;
                    if (daysDifference > 365)
                    {
                        return BadRequest(new ApiResponse
                        {
                            Success = false,
                            Message = "Validation failed",
                            Errors = ["Date range cannot exceed 1 year"]
                        });
                    }
                }

                var result = await _weeklyLogService.GetWeeklyLogsAsync(userId, request);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<PagedResult<WeeklyLogResponse>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving weekly logs");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "WeeklyLog Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving weekly logs",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get a specific weeklylog
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<WeeklyLogResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetWeeklyLogById([FromRoute] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Weeklylog ID must be greater than 0"]
                    });
                }

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

                var result = await _weeklyLogService.GetWeeklyLogByIdAsync(id, userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<WeeklyLogResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving weekly log {WeeklyLogId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Weekly Log Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving the weekly log",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Create a new weeklylog
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<WeeklyLogResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> CreateWeeklyLog([FromBody] WeeklyLogRequest request)
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

                var result = await _weeklyLogService.CreateWeeklyLogAsync(request, userId);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                var locationUri = Url.Action(nameof(GetWeeklyLogById), new { id = result.Data!.Id });

                return Created(locationUri, new ApiResponse<WeeklyLogResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating weekly log");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "WeeklyLog Creation Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while creating the weekly log",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Update an existing weeklylog
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<WeeklyLogResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> UpdateWeeklyLog([FromRoute] int id, [FromBody] WeeklyLogRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Weeklylog ID must be greater than 0"]
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

                var result = await _weeklyLogService.UpdateWeeklyLogAsync(id, request, userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<WeeklyLogResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating weekly log {WeeklyLogId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "WeeklyLog Update Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while updating the weekly log",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Delete a weeklylog
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> DeleteWeeklyLog([FromRoute] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Weeklylog ID must be greater than 0"]
                    });
                }

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

                var result = await _weeklyLogService.DeleteWeeklyLogAsync(id, userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
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
                _logger.LogError(ex, "Unexpected error deleting weeklylog {WeeklyLogId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "WeeklyLog Deletion Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while deleting the weeklylog",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        [HttpPatch("{id}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] UpdateStatusRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }
            
            var result = await _weeklyLogService.UpdateStatusAsync(id, request.Status, userId);
            
            if (!result.Success)
            {
                return BadRequest(new ApiResponse { Success = false, Message = result.Message });
            }
            
            return Ok(new ApiResponse { Success = true, Message = result.Message });
        }
    }
}