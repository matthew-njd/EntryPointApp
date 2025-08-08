using System.Security.Claims;
using EntryPointApp.Api.Models.Common;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;
using EntryPointApp.Api.Services.Timesheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntryPointApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimesheetController(ITimesheetService timesheetService, ILogger<TimesheetController> logger) : ControllerBase
    {
        private readonly ITimesheetService _timesheetService = timesheetService;
        private readonly ILogger<TimesheetController> _logger = logger;

        /// <summary>
        /// Get all timesheets for the authenticated user with pagination and optional date filtering
        /// </summary>
        /// <param name="request">Pagination and date filter parameters</param>
        /// <returns>Paginated list of user's timesheets</returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TimesheetResponse>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetTimesheets([FromQuery] PagedRequest request)
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
                    request.StartDate = request.EndDate.Value.AddMonths(-3);

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

                // Validate date range doesn't exceed 1 year
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

                var result = await _timesheetService.GetTimesheetsAsync(userId, request);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<PagedResult<TimesheetResponse>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving timesheets for user");
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
        /// Get a specific timesheet by ID for the authenticated user
        /// </summary>
        /// <param name="id">Timesheet ID</param>
        /// <returns>Timesheet details</returns>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<TimesheetResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetTimesheetById([FromRoute] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Timesheet ID must be greater than 0"]
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

                var result = await _timesheetService.GetTimesheetByIdAsync(id, userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<TimesheetResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving timesheet {TimesheetId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Timesheet Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving the timesheet",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Create a new timesheet for the authenticated user
        /// </summary>
        /// <param name="request">Timesheet data</param>
        /// <returns>Created timesheet details</returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<TimesheetResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> CreateTimesheet([FromBody] TimesheetRequest request)
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

                var result = await _timesheetService.CreateTimesheetAsync(request, userId);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                // Add Location header pointing to the new resource
                var locationUri = Url.Action(nameof(GetTimesheetById), new { id = result.Data!.Id });

                return Created(locationUri, new ApiResponse<TimesheetResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating timesheet for user");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Timesheet Creation Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while creating the timesheet",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Update an existing timesheet for the authenticated user
        /// </summary>
        /// <param name="id">Timesheet ID</param>
        /// <param name="request">Updated timesheet data</param>
        /// <returns>Updated timesheet details</returns>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<TimesheetResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> UpdateTimesheet([FromRoute] int id, [FromBody] TimesheetRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Timesheet ID must be greater than 0"]
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

                var result = await _timesheetService.UpdateTimesheetAsync(id, request, userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<TimesheetResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating timesheet {TimesheetId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Timesheet Update Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while updating the timesheet",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }
        
        /// <summary>
        /// Delete a timesheet for the authenticated user
        /// </summary>
        /// <param name="id">Timesheet ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> DeleteTimesheet([FromRoute] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Timesheet ID must be greater than 0"]
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

                var result = await _timesheetService.DeleteTimesheetAsync(id, userId);

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
                _logger.LogError(ex, "Unexpected error deleting timesheet {TimesheetId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Timesheet Deletion Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while deleting the timesheet",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }
    }
}