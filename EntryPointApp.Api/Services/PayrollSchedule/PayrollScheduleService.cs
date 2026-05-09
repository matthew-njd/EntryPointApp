using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.PayrollSchedule;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.PayrollSchedule
{
    public class PayrollScheduleService(ApplicationDbContext context) : IPayrollScheduleService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<PayrollScheduleListResult> GetAllAsync()
        {
            var schedules = await _context.PayrollSchedules
                .OrderBy(s => s.DateFrom)
                .Select(s => new PayrollScheduleResponse
                {
                    Id = s.Id,
                    DateFrom = s.DateFrom,
                    DateTo = s.DateTo,
                    PayrollDate = s.PayrollDate
                })
                .ToListAsync();

            return new PayrollScheduleListResult
            {
                Success = true,
                Message = "Payroll schedules retrieved",
                Data = schedules
            };
        }

        public async Task<PayrollScheduleResult> CreateAsync(DateOnly dateFrom, DateOnly dateTo, DateOnly payrollDate)
        {
            var now = DateTime.UtcNow;
            var schedule = new Models.Entities.PayrollSchedule
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                PayrollDate = payrollDate,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.PayrollSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return new PayrollScheduleResult
            {
                Success = true,
                Message = "Payroll schedule entry created",
                Data = ToResponse(schedule)
            };
        }

        public async Task<PayrollScheduleResult> UpdateAsync(int id, DateOnly dateFrom, DateOnly dateTo, DateOnly payrollDate)
        {
            var schedule = await _context.PayrollSchedules.FindAsync(id);
            if (schedule == null)
                return new PayrollScheduleResult { Success = false, Message = "Schedule entry not found" };

            schedule.DateFrom = dateFrom;
            schedule.DateTo = dateTo;
            schedule.PayrollDate = payrollDate;
            schedule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new PayrollScheduleResult
            {
                Success = true,
                Message = "Payroll schedule entry updated",
                Data = ToResponse(schedule)
            };
        }

        public async Task<BasePayrollScheduleResult> DeleteAsync(int id)
        {
            var schedule = await _context.PayrollSchedules.FindAsync(id);
            if (schedule == null)
                return new BasePayrollScheduleResult { Success = false, Message = "Schedule entry not found" };

            _context.PayrollSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return new BasePayrollScheduleResult { Success = true, Message = "Payroll schedule entry deleted" };
        }

        public async Task<PayrollScheduleImportResult> BulkImportAsync(List<PayrollScheduleImportItem> entries, bool replace)
        {
            if (replace)
            {
                var existing = await _context.PayrollSchedules.ToListAsync();
                _context.PayrollSchedules.RemoveRange(existing);
            }

            var now = DateTime.UtcNow;
            var schedules = entries.Select(e => new Models.Entities.PayrollSchedule
            {
                DateFrom = e.DateFrom,
                DateTo = e.DateTo,
                PayrollDate = e.PayrollDate,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

            _context.PayrollSchedules.AddRange(schedules);
            await _context.SaveChangesAsync();

            return new PayrollScheduleImportResult
            {
                Success = true,
                Message = $"{schedules.Count} payroll schedule entries imported",
                Data = new PayrollScheduleImportStats { Imported = schedules.Count }
            };
        }

        public async Task<PayrollScheduleLookupResult> LookupAsync(DateOnly timesheetDateFrom)
        {
            var schedule = await _context.PayrollSchedules
                .Where(s => s.DateFrom <= timesheetDateFrom && timesheetDateFrom <= s.DateTo)
                .FirstOrDefaultAsync();

            return new PayrollScheduleLookupResult
            {
                Success = true,
                Message = schedule != null ? "Payroll date found" : "No payroll schedule found for this date",
                Data = new PayrollScheduleLookupResponse { PayrollDate = schedule?.PayrollDate, DeadlineDate = schedule?.DateTo }
            };
        }

        private static PayrollScheduleResponse ToResponse(Models.Entities.PayrollSchedule s) => new()
        {
            Id = s.Id,
            DateFrom = s.DateFrom,
            DateTo = s.DateTo,
            PayrollDate = s.PayrollDate
        };
    }
}
