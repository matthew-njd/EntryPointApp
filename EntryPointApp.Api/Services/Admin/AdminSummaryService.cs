using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Admin;
using EntryPointApp.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.Admin
{
    public class AdminSummaryService(
        ApplicationDbContext context,
        ILogger<AdminSummaryService> logger) : IAdminSummaryService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<AdminSummaryService> _logger = logger;

        public async Task<PayrollSummaryResult> GetPayrollSummaryAsync(DateOnly dateFrom, DateOnly dateTo)
        {
            try
            {
                _logger.LogInformation("Generating payroll summary for {DateFrom} to {DateTo}", dateFrom, dateTo);

                var weeklyLogs = await _context.Timesheet_WeeklyLogs
                    .Include(w => w.User)
                    .Include(w => w.DailyLogs.Where(d => !d.IsDeleted))
                    .Where(w =>
                        !w.IsDeleted &&
                        w.Status == TimesheetStatus.Approved &&
                        w.DateFrom >= dateFrom &&
                        w.DateTo <= dateTo)
                    .ToListAsync();

                var grouped = weeklyLogs.GroupBy(w => w.UserId);

                var items = new List<PayrollSummaryItemDto>();

                foreach (var group in grouped.OrderBy(g => g.First().User.LastName).ThenBy(g => g.First().User.FirstName))
                {
                    var user = group.First().User;
                    var totalHours = group.Sum(w => w.TotalHours);
                    var allDailyLogs = group.SelectMany(w => w.DailyLogs).ToList();
                    var totalMileage = allDailyLogs.Sum(d => d.Mileage);
                    var totalTollCharges = allDailyLogs.Sum(d => d.TollCharge);
                    var totalParkingFees = allDailyLogs.Sum(d => d.ParkingFee);
                    var totalOtherCharges = allDailyLogs.Sum(d => d.OtherCharges);

                    var userRate = await _context.Timesheet_UserRates
                        .Where(r => r.UserId == user.Id && r.EffectiveDate <= dateFrom)
                        .OrderByDescending(r => r.EffectiveDate)
                        .FirstOrDefaultAsync();

                    var hourlyRate = userRate?.HourlyRate ?? 0m;
                    var mileageRate = userRate?.MileageRate ?? 0m;

                    items.Add(new PayrollSummaryItemDto
                    {
                        UserId = user.Id,
                        FullName = $"{user.FirstName} {user.LastName}".Trim(),
                        EmployeeType = user.EmployeeType?.ToString() ?? "—",
                        HourlyRate = hourlyRate,
                        MileageRate = mileageRate,
                        TotalHours = totalHours,
                        TotalMileage = totalMileage,
                        TotalTollCharges = totalTollCharges,
                        TotalParkingFees = totalParkingFees,
                        TotalOtherCharges = totalOtherCharges,
                        GrossPay = hourlyRate * totalHours,
                        MileageReimbursement = mileageRate * totalMileage
                    });
                }

                return new PayrollSummaryResult
                {
                    Success = true,
                    Message = $"Found {items.Count} employee(s) with approved timesheets.",
                    Data = new PayrollSummaryResponse
                    {
                        DateFrom = dateFrom,
                        DateTo = dateTo,
                        Items = items
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payroll summary for {DateFrom} to {DateTo}", dateFrom, dateTo);
                return new PayrollSummaryResult
                {
                    Success = false,
                    Message = "An error occurred while generating the payroll summary.",
                    Errors = [ex.Message]
                };
            }
        }
    }
}
