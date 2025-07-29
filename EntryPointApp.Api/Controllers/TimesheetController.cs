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
                    request.EndDate = DateTime.UtcNow.Date;
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
                    var daysDifference = (request.EndDate.Value - request.StartDate.Value).TotalDays;
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
    }
}