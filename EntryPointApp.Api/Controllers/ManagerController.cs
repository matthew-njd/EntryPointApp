using System.Security.Claims;
using EntryPointApp.Api.Models.Common;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Manager;
using EntryPointApp.Api.Services.Manager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntryPointApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public class ManagerController(IManagerService managerService, ILogger<ManagerController> logger) : ControllerBase
    {
        private readonly IManagerService _managerService = managerService;
        private readonly ILogger<ManagerController> _logger = logger;

        /// <summary>
        /// Get all team timesheets with pagination, optionally filtered by status.
        /// </summary>
        [HttpGet("timesheets")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TeamTimesheetResponse>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetTeamTimesheets(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = "All")
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user context",
                        Errors = ["Unable to identify user"]
                    });
                }

                if (page < 1)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Page number must be 1 or greater"]
                    });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Page size must be between 1 and 100"]
                    });
                }

                var result = await _managerService.GetTeamTimesheetsAsync(managerId, page, pageSize, status);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Manager {ManagerId} retrieved team timesheets - Page {Page} with filter {Filter}", managerId, page, status);

                return Ok(new ApiResponse<PagedResult<TeamTimesheetResponse>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving team timesheets");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Team Timesheets Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving team timesheets",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get only pending timesheets — the manager's action queue.
        /// </summary>
        [HttpGet("timesheets/pending")]
        [ProducesResponseType(typeof(ApiResponse<List<TeamTimesheetResponse>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetPendingTimesheets()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user context",
                        Errors = ["Unable to identify user"]
                    });
                }

                var result = await _managerService.GetPendingTimesheetsAsync(managerId);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Manager {ManagerId} retrieved {Count} pending timesheets", managerId, result.Data?.Count ?? 0);

                return Ok(new ApiResponse<List<TeamTimesheetResponse>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving pending timesheets");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Pending Timesheets Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving pending timesheets",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get full timesheet detail including all daily logs.
        /// </summary>
        [HttpGet("timesheets/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<TeamTimesheetDetailResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetTimesheetDetail([FromRoute] int id)
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
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user context",
                        Errors = ["Unable to identify user"]
                    });
                }

                var result = await _managerService.GetTimesheetDetailAsync(id, managerId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Manager {ManagerId} retrieved timesheet detail {TimesheetId}", managerId, id);

                return Ok(new ApiResponse<TeamTimesheetDetailResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving timesheet detail {TimesheetId}", id);
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

        /// <summary>
        /// Approve a pending timesheet with an optional comment.
        /// </summary>
        [HttpPut("timesheets/{id:int}/approve")]
        [ProducesResponseType(typeof(ApiResponse<TeamTimesheetResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> ApproveTimesheet([FromRoute] int id, [FromBody] ApproveTimesheetRequest request)
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
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user context",
                        Errors = ["Unable to identify user"]
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

                var result = await _managerService.ApproveTimesheetAsync(id, managerId, request);

                if (!result.Success)
                {
                    if (result.Errors.Any(e => e.Contains("does not exist")))
                    {
                        return NotFound(new ApiResponse
                        {
                            Success = false,
                            Message = result.Message,
                            Errors = result.Errors
                        });
                    }

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Manager {ManagerId} approved timesheet {TimesheetId}", managerId, id);

                return Ok(new ApiResponse<TeamTimesheetResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error approving timesheet {TimesheetId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Timesheet Approval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while approving the timesheet",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Deny a pending timesheet with a required reason.
        /// </summary>
        [HttpPut("timesheets/{id:int}/deny")]
        [ProducesResponseType(typeof(ApiResponse<TeamTimesheetResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> DenyTimesheet([FromRoute] int id, [FromBody] DenyTimesheetRequest request)
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
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user context",
                        Errors = ["Unable to identify user"]
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

                var result = await _managerService.DenyTimesheetAsync(id, managerId, request);

                if (!result.Success)
                {
                    if (result.Errors.Any(e => e.Contains("does not exist")))
                    {
                        return NotFound(new ApiResponse
                        {
                            Success = false,
                            Message = result.Message,
                            Errors = result.Errors
                        });
                    }

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Manager {ManagerId} denied timesheet {TimesheetId}", managerId, id);

                return Ok(new ApiResponse<TeamTimesheetResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error denying timesheet {TimesheetId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Timesheet Denial Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while denying the timesheet",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }
    }
}