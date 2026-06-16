using System.Security.Claims;
using EntryPointApp.Api.Models.Common;
using EntryPointApp.Api.Models.Dtos.Admin;
using EntryPointApp.Api.Services.Admin;
using EntryPointApp.Api.Services.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntryPointApp.Api.Controllers
{
    [ApiController]
    [Route("api/admin/payroll-summary")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminSummaryController(
        IAdminSummaryService summaryService,
        IExcelService excelService,
        ILogger<AdminSummaryController> logger) : ControllerBase
    {
        private readonly IAdminSummaryService _summaryService = summaryService;
        private readonly IExcelService _excelService = excelService;
        private readonly ILogger<AdminSummaryController> _logger = logger;

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PayrollSummaryResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> GetSummary([FromQuery] string dateFrom, [FromQuery] string dateTo)
        {
            if (!DateOnly.TryParse(dateFrom, out var from) || !DateOnly.TryParse(dateTo, out var to))
                return BadRequest(new ApiResponse { Success = false, Message = "Invalid date format. Use yyyy-MM-dd." });

            if (from > to)
                return BadRequest(new ApiResponse { Success = false, Message = "dateFrom must be on or before dateTo." });

            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _summaryService.GetPayrollSummaryAsync(from, to);

            if (!result.Success)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }

            _logger.LogInformation("Admin {AdminId} retrieved payroll summary for {DateFrom} to {DateTo}", adminId, from, to);

            return Ok(new ApiResponse<PayrollSummaryResponse>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            });
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportExcel([FromQuery] string dateFrom, [FromQuery] string dateTo)
        {
            if (!DateOnly.TryParse(dateFrom, out var from) || !DateOnly.TryParse(dateTo, out var to))
                return BadRequest(new ApiResponse { Success = false, Message = "Invalid date format. Use yyyy-MM-dd." });

            if (from > to)
                return BadRequest(new ApiResponse { Success = false, Message = "dateFrom must be on or before dateTo." });

            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _summaryService.GetPayrollSummaryAsync(from, to);

            if (!result.Success || result.Data == null)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }

            var bytes = await _excelService.GeneratePayrollSummaryExcelAsync(result.Data);

            _logger.LogInformation("Admin {AdminId} exported payroll summary Excel for {DateFrom} to {DateTo}", adminId, from, to);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"PayrollSummary_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.xlsx"
            );
        }
    }
}
