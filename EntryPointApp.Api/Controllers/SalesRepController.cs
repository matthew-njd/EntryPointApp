using System.Security.Claims;
using EntryPointApp.Api.Models.Common;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Manager;
using EntryPointApp.Api.Services.Authentication;
using EntryPointApp.Api.Services.SalesRep;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntryPointApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.SalesRepOnly)]
    public class SalesRepController(ISalesRepService salesRepService, ILogger<SalesRepController> logger) : ControllerBase
    {
        private readonly ISalesRepService _salesRepService = salesRepService;
        private readonly ILogger<SalesRepController> _logger = logger;

        /// <summary>
        /// Get all client timesheets with pagination, optionally filtered by status.
        /// </summary>
        [HttpGet("timesheets")]
        [ProducesResponseType(typeof(ApiResponse<TeamTimesheetPagedResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetTeamTimesheets(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = "All",
            [FromQuery] string? search = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int salesRepId))
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

                var result = await _salesRepService.GetTeamTimesheetsAsync(salesRepId, page, pageSize, status, search);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Sales rep {SalesRepId} retrieved client timesheets - Page {Page} with filter {Filter}", salesRepId, page, status);

                return Ok(new ApiResponse<TeamTimesheetPagedResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving client timesheets");
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Client Timesheets Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving client timesheets",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get all pending timesheets awaiting sales rep review.
        /// </summary>
        [HttpGet("timesheets/pending")]
        [ProducesResponseType(typeof(ApiResponse<List<TeamTimesheetResponse>>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetPendingTimesheets()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int salesRepId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user context",
                        Errors = ["Unable to identify user"]
                    });
                }

                var result = await _salesRepService.GetPendingTimesheetsAsync(salesRepId);

                _logger.LogInformation("Sales rep {SalesRepId} retrieved pending timesheets", salesRepId);

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
        /// Get full detail for a specific timesheet.
        /// </summary>
        [HttpGet("timesheets/{id}")]
        [ProducesResponseType(typeof(ApiResponse<TeamTimesheetDetailResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetTimesheetDetail([FromRoute] int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int salesRepId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user context",
                        Errors = ["Unable to identify user"]
                    });
                }

                if (id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Timesheet ID must be greater than 0"]
                    });
                }

                var result = await _salesRepService.GetTimesheetDetailAsync(id, salesRepId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Sales rep {SalesRepId} retrieved timesheet detail {TimesheetId}", salesRepId, id);

                return Ok(new ApiResponse<TeamTimesheetDetailResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving timesheet detail {Id}", id);
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
        /// Approve a timesheet — forwards it to the manager for final approval.
        /// </summary>
        [HttpPut("timesheets/{id}/approve")]
        [ProducesResponseType(typeof(ApiResponse<TeamTimesheetResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> ApproveTimesheet([FromRoute] int id, [FromBody] ApproveTimesheetRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int salesRepId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user context",
                        Errors = ["Unable to identify user"]
                    });
                }

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

                var result = await _salesRepService.ApproveTimesheetAsync(id, salesRepId, request);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Sales rep {SalesRepId} approved timesheet {TimesheetId}", salesRepId, id);

                return Ok(new ApiResponse<TeamTimesheetResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error approving timesheet {Id}", id);
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
        /// Deny a timesheet with a required reason.
        /// </summary>
        [HttpPut("timesheets/{id}/deny")]
        [ProducesResponseType(typeof(ApiResponse<TeamTimesheetResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> DenyTimesheet([FromRoute] int id, [FromBody] DenyTimesheetRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int salesRepId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user context",
                        Errors = ["Unable to identify user"]
                    });
                }

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

                var result = await _salesRepService.DenyTimesheetAsync(id, salesRepId, request);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogInformation("Sales rep {SalesRepId} denied timesheet {TimesheetId}", salesRepId, id);

                return Ok(new ApiResponse<TeamTimesheetResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error denying timesheet {Id}", id);
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
