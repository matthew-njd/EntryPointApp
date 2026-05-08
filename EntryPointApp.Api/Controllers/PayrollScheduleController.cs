using System.Globalization;
using System.Security.Claims;
using EntryPointApp.Api.Models.Common;
using EntryPointApp.Api.Models.Dtos.PayrollSchedule;
using EntryPointApp.Api.Services.PayrollSchedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntryPointApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class PayrollScheduleController(
        IPayrollScheduleService payrollScheduleService,
        ILogger<PayrollScheduleController> logger) : ControllerBase
    {
        private readonly IPayrollScheduleService _service = payrollScheduleService;
        private readonly ILogger<PayrollScheduleController> _logger = logger;

        // ── Authenticated user endpoint ──────────────────────────────────────

        [HttpGet("api/payroll-schedule/lookup")]
        [ProducesResponseType(typeof(ApiResponse<PayrollScheduleLookupResponse>), 200)]
        public async Task<IActionResult> Lookup([FromQuery] string dateFrom)
        {
            if (!DateOnly.TryParse(dateFrom, out var date))
                return BadRequest(new ApiResponse { Success = false, Message = "Invalid dateFrom format. Use yyyy-MM-dd." });

            var result = await _service.LookupAsync(date);

            return Ok(new ApiResponse<PayrollScheduleLookupResponse>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            });
        }

        // ── Admin-only endpoints ─────────────────────────────────────────────

        [HttpGet("api/admin/payroll-schedule")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(ApiResponse<List<PayrollScheduleResponse>>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            return Ok(new ApiResponse<List<PayrollScheduleResponse>>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            });
        }

        [HttpPost("api/admin/payroll-schedule")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(ApiResponse<PayrollScheduleResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> Create([FromBody] PayrollScheduleRequest request)
        {
            if (request.DateTo < request.DateFrom)
                return BadRequest(new ApiResponse { Success = false, Message = "DateTo must be on or after DateFrom." });

            var result = await _service.CreateAsync(request.DateFrom, request.DateTo, request.PayrollDate);

            if (!result.Success)
                return BadRequest(new ApiResponse { Success = false, Message = result.Message });

            _logger.LogInformation("Admin {AdminId} created payroll schedule entry {Id}",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value, result.Data?.Id);

            return Ok(new ApiResponse<PayrollScheduleResponse>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            });
        }

        [HttpPut("api/admin/payroll-schedule/{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(ApiResponse<PayrollScheduleResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Update(int id, [FromBody] PayrollScheduleRequest request)
        {
            if (request.DateTo < request.DateFrom)
                return BadRequest(new ApiResponse { Success = false, Message = "DateTo must be on or after DateFrom." });

            var result = await _service.UpdateAsync(id, request.DateFrom, request.DateTo, request.PayrollDate);

            if (!result.Success)
                return NotFound(new ApiResponse { Success = false, Message = result.Message });

            return Ok(new ApiResponse<PayrollScheduleResponse>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            });
        }

        [HttpDelete("api/admin/payroll-schedule/{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result.Success)
                return NotFound(new ApiResponse { Success = false, Message = result.Message });

            _logger.LogInformation("Admin {AdminId} deleted payroll schedule entry {Id}",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);

            return Ok(new ApiResponse { Success = true, Message = result.Message });
        }

        [HttpPost("api/admin/payroll-schedule/import")]
        [Authorize(Policy = "AdminOnly")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<PayrollScheduleImportStats>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> Import(IFormFile file, [FromQuery] bool replace = true)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new ApiResponse { Success = false, Message = "No file uploaded." });

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new ApiResponse { Success = false, Message = "Only .csv files are accepted." });

            var entries = new List<PayrollScheduleImportItem>();
            var errors = new List<string>();

            using var reader = new StreamReader(file.OpenReadStream());
            var allLines = (await reader.ReadToEndAsync()).Split('\n');
            var lineNumber = 0;

            foreach (var rawLine in allLines)
            {
                var line = rawLine.TrimEnd('\r');
                lineNumber++;

                if (lineNumber == 1) continue; // skip header

                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 3)
                {
                    errors.Add($"Line {lineNumber}: expected 3 columns, got {parts.Length}.");
                    continue;
                }

                var formats = new[] { "d-MMM-yy", "d-MMM-yyyy", "yyyy-MM-dd", "MM/dd/yyyy" };

                if (!DateOnly.TryParseExact(parts[0].Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateFrom) ||
                    !DateOnly.TryParseExact(parts[1].Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTo) ||
                    !DateOnly.TryParseExact(parts[2].Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var payrollDate))
                {
                    errors.Add($"Line {lineNumber}: could not parse dates \"{parts[0].Trim()}\", \"{parts[1].Trim()}\", \"{parts[2].Trim()}\".");
                    continue;
                }

                entries.Add(new PayrollScheduleImportItem(dateFrom, dateTo, payrollDate));
            }

            if (errors.Count > 0 && entries.Count == 0)
                return BadRequest(new ApiResponse { Success = false, Message = "CSV parsing failed.", Errors = errors });

            var result = await _service.BulkImportAsync(entries, replace);

            _logger.LogInformation("Admin {AdminId} imported {Count} payroll schedule entries (replace={Replace})",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value, result.Data?.Imported, replace);

            return Ok(new ApiResponse<PayrollScheduleImportStats>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data,
                Errors = errors
            });
        }
    }
}
