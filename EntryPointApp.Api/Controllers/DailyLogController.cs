using System.Security.Claims;
using EntryPointApp.Api.Models.Common;
using EntryPointApp.Api.Models.Dtos.DailyLog;
using EntryPointApp.Api.Services.DailyLog;
using EntryPointApp.Api.Services.Receipt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntryPointApp.Api.Controllers
{
    [ApiController]
    [Route("api/weeklylogs/{weeklyLogId}/[controller]")]
    public class DailyLogController(IDailyLogService dailyLogService, IReceiptService receiptService, ILogger<DailyLogController> logger) : ControllerBase
    {
        private readonly IDailyLogService _dailyLogService = dailyLogService;
        private readonly IReceiptService _receiptService = receiptService;
        private readonly ILogger<DailyLogController> _logger = logger;

        /// <summary>
        /// Get all dailylogs for a specific weeklylog
        /// </summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<DailyLogResponse>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetDailyLogs([FromRoute] int weeklyLogId)
        {
            try
            {
                if (weeklyLogId <= 0)
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

                var result = await _dailyLogService.GetDailyLogsByWeeklyLogIdAsync(weeklyLogId, userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<List<DailyLogResponse>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving daily logs for weekly log {WeeklyLogId}", weeklyLogId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "DailyLog Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving daily logs",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get a specific dailylog by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<DailyLogResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetDailyLogById([FromRoute] int weeklyLogId, [FromRoute] int id)
        {
            try
            {
                if (weeklyLogId <= 0 || id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["IDs must be greater than 0"]
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

                var result = await _dailyLogService.GetDailyLogByIdAsync(id, weeklyLogId, userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<DailyLogResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving dailylog {DailyLogId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "DailyLog Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving the daily log",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Create a new dailylog for a weeklylog
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<DailyLogResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> CreateDailyLog([FromRoute] int weeklyLogId, [FromBody] DailyLogRequest request)
        {
            try
            {
                if (weeklyLogId <= 0)
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

                var result = await _dailyLogService.CreateDailyLogAsync(weeklyLogId, request, userId);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                var locationUri = Url.Action(nameof(GetDailyLogById), new { weeklyLogId, id = result.Data!.Id });

                return Created(locationUri, new ApiResponse<DailyLogResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating daily log for weeklylog {WeeklyLogId}", weeklyLogId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "DailyLog Creation Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while creating the dailylog",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Create multiple dailylogs for a weeklylog in a single request
        /// </summary>
        [HttpPost("batch")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<DailyLogResponse>>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> CreateDailyLogsBatch([FromRoute] int weeklyLogId, [FromBody] List<DailyLogRequest> requests)
        {
            try
            {
                if (weeklyLogId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Weeklylog ID must be greater than 0"]
                    });
                }

                if (requests == null || requests.Count == 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["At least one daily log is required"]
                    });
                }

                if (requests.Count > 7)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Cannot create more than 7 daily logs at once"]
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

                var result = await _dailyLogService.CreateDailyLogsBatchAsync(weeklyLogId, requests, userId);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Created($"/api/weeklylogs/{weeklyLogId}/dailylog", new ApiResponse<List<DailyLogResponse>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating daily logs batch for weeklylog {WeeklyLogId}", weeklyLogId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "DailyLog Batch Creation Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while creating the daily logs",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Update an existing dailylog
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<DailyLogResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> UpdateDailyLog([FromRoute] int weeklyLogId, [FromRoute] int id, [FromBody] DailyLogRequest request)
        {
            try
            {
                if (weeklyLogId <= 0 || id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["IDs must be greater than 0"]
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

                var result = await _dailyLogService.UpdateDailyLogAsync(id, weeklyLogId, request, userId);

                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                return Ok(new ApiResponse<DailyLogResponse>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating dailylog {DailyLogId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Daily Log Update Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while updating the dailylog",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Delete a dailylog
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> DeleteDailyLog([FromRoute] int weeklyLogId, [FromRoute] int id)
        {
            try
            {
                if (weeklyLogId <= 0 || id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["IDs must be greater than 0"]
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

                var result = await _dailyLogService.DeleteDailyLogAsync(id, weeklyLogId, userId);

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
                _logger.LogError(ex, "Unexpected error deleting dailylog {DailyLogId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "DailyLog Deletion Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while deleting the dailylog",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Upload a receipt for a specific dailylog
        /// </summary>
        [HttpPost("{id}/receipts")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ReceiptResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> UploadReceipt([FromRoute] int weeklyLogId, [FromRoute] int id, IFormFile file)
        {
            try
            {
                if (weeklyLogId <= 0 || id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["IDs must be greater than 0"]
                    });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["A file must be provided"]
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

                var result = await _receiptService.UploadReceiptAsync(id, weeklyLogId, file, userId);

                if (!result.Success)
                {
                    if (result.Errors?.Any(e => e.Contains("permission")) == true)
                    {
                        return StatusCode(403, new ApiResponse
                        {
                            Success = false,
                            Message = result.Message,
                            Errors = result.Errors
                        });
                    }

                    if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    {
                        return NotFound(new ApiResponse
                        {
                            Success = false,
                            Message = result.Message,
                            Errors = result.Errors ?? []
                        });
                    }

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors ?? []
                    });
                }

                return Created(
                    Url.Action(nameof(DownloadReceipt), new { weeklyLogId, id, attachmentId = result.Data!.Id }),
                    new ApiResponse<ReceiptResponse>
                    {
                        Success = true,
                        Message = result.Message,
                        Data = result.Data
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error uploading receipt for daily log {DailyLogId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Receipt Upload Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while uploading the receipt",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Get all receipts for a specific dailylog
        /// </summary>
        [HttpGet("{id}/receipts")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<ReceiptResponse>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetReceipts([FromRoute] int weeklyLogId, [FromRoute] int id)
        {
            try
            {
                if (weeklyLogId <= 0 || id <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["IDs must be greater than 0"]
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

                var result = await _receiptService.GetReceiptsAsync(id, weeklyLogId, userId);

                if (!result.Success)
                {
                    if (result.Errors?.Any(e => e.Contains("permission")) == true)
                    {
                        return StatusCode(403, new ApiResponse
                        {
                            Success = false,
                            Message = result.Message,
                            Errors = result.Errors
                        });
                    }

                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors ?? []
                    });
                }

                return Ok(new ApiResponse<List<ReceiptResponse>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving receipts for daily log {DailyLogId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Receipt Retrieval Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while retrieving receipts",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Download a specific receipt file
        /// </summary>
        [HttpGet("{id}/receipts/{attachmentId}")]
        [Authorize]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> DownloadReceipt([FromRoute] int weeklyLogId, [FromRoute] int id, [FromRoute] int attachmentId)
        {
            try
            {
                if (weeklyLogId <= 0 || id <= 0 || attachmentId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["IDs must be greater than 0"]
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

                var result = await _receiptService.DownloadReceiptAsync(attachmentId, id, weeklyLogId, userId);

                if (!result.Success)
                {
                    if (result.Errors?.Any(e => e.Contains("permission")) == true)
                    {
                        return StatusCode(403, new ApiResponse
                        {
                            Success = false,
                            Message = result.Message,
                            Errors = result.Errors
                        });
                    }

                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors ?? []
                    });
                }

                return File(result.FileStream!, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error downloading receipt {AttachmentId}", attachmentId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Receipt Download Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while downloading the receipt",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Delete a receipt (owner or admin only)
        /// </summary>
        [HttpDelete("{id}/receipts/{attachmentId}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> DeleteReceipt([FromRoute] int weeklyLogId, [FromRoute] int id, [FromRoute] int attachmentId)
        {
            try
            {
                if (weeklyLogId <= 0 || id <= 0 || attachmentId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["IDs must be greater than 0"]
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

                var result = await _receiptService.DeleteReceiptAsync(attachmentId, id, weeklyLogId, userId);

                if (!result.Success)
                {
                    if (result.Errors?.Any(e => e.Contains("permission") || e.Contains("Only the owner")) == true)
                    {
                        return StatusCode(403, new ApiResponse
                        {
                            Success = false,
                            Message = result.Message,
                            Errors = result.Errors
                        });
                    }

                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors ?? []
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
                _logger.LogError(ex, "Unexpected error deleting receipt {AttachmentId}", attachmentId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "Receipt Deletion Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while deleting the receipt",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Update multiple dailylogs for a weeklylog (sync operation)
        /// Creates new logs, updates existing ones, and removes logs not in the request
        /// </summary>
        [HttpPut]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<DailyLogResponse>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> UpdateDailyLogs([FromRoute] int weeklyLogId, [FromBody] UpdateDailyLogsRequest request)
        {
            try
            {
                if (weeklyLogId <= 0)
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

                var result = await _dailyLogService.UpdateDailyLogsAsync(weeklyLogId, request, userId);

                if (!result.Success)
                {
                    if (result.Errors?.Any(e => e.Contains("Draft status")) == true)
                    {
                        return StatusCode(403, new ApiResponse
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
                        Errors = result.Errors ?? []
                    });
                }

                return Ok(new ApiResponse<List<DailyLogResponse>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating daily logs for weeklylog {WeeklyLogId}", weeklyLogId);
                return StatusCode(500, new ErrorResponse
                {
                    Type = "InternalServerError",
                    Title = "DailyLog Update Error",
                    Status = 500,
                    Detail = "An unexpected error occurred while updating the daily logs",
                    Instance = HttpContext.Request.Path,
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }
    }
}