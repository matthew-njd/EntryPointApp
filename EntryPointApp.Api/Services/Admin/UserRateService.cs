using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Users;
using EntryPointApp.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.Admin
{
    public class UserRateService(
        ApplicationDbContext context,
        ILogger<UserRateService> logger) : IUserRateService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<UserRateService> _logger = logger;

        public async Task<UserRateListResult> GetRatesForUserAsync(int userId)
        {
            try
            {
                var userExists = await _context.Timesheet_Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return new UserRateListResult
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = ["The requested user does not exist"]
                    };
                }

                var rates = await _context.Timesheet_UserRates
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.EffectiveDate)
                    .Select(r => new UserRateDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        HourlyRate = r.HourlyRate,
                        MileageRate = r.MileageRate,
                        EffectiveDate = r.EffectiveDate,
                        CreatedAt = r.CreatedAt,
                        CreatedByAdminId = r.CreatedByAdminId
                    })
                    .ToListAsync();

                return new UserRateListResult
                {
                    Success = true,
                    Message = "Rates retrieved successfully!",
                    Data = rates
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rates for user {UserId}", userId);
                return new UserRateListResult
                {
                    Success = false,
                    Message = "Failed to retrieve rates",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<UserRateResult> GetCurrentRateAsync(int userId)
        {
            try
            {
                var userExists = await _context.Timesheet_Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return new UserRateResult
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = ["The requested user does not exist"]
                    };
                }

                var now = DateTime.UtcNow;
                var rate = await _context.Timesheet_UserRates
                    .Where(r => r.UserId == userId && r.EffectiveDate <= now)
                    .OrderByDescending(r => r.EffectiveDate)
                    .Select(r => new UserRateDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        HourlyRate = r.HourlyRate,
                        MileageRate = r.MileageRate,
                        EffectiveDate = r.EffectiveDate,
                        CreatedAt = r.CreatedAt,
                        CreatedByAdminId = r.CreatedByAdminId
                    })
                    .FirstOrDefaultAsync();

                if (rate == null)
                {
                    return new UserRateResult
                    {
                        Success = false,
                        Message = "No current rate found",
                        Errors = ["No rate is in effect for this user"]
                    };
                }

                return new UserRateResult
                {
                    Success = true,
                    Message = "Current rate retrieved successfully!",
                    Data = rate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current rate for user {UserId}", userId);
                return new UserRateResult
                {
                    Success = false,
                    Message = "Failed to retrieve current rate",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<UserRateResult> SetRateAsync(int userId, SetUserRateRequest request, int adminId)
        {
            try
            {
                var userExists = await _context.Timesheet_Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return new UserRateResult
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = ["The requested user does not exist"]
                    };
                }

                var rate = new UserRate
                {
                    UserId = userId,
                    HourlyRate = request.HourlyRate,
                    MileageRate = request.MileageRate,
                    EffectiveDate = request.EffectiveDate.Date,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAdminId = adminId
                };

                _context.Timesheet_UserRates.Add(rate);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Admin {AdminId} set rate for user {UserId}: hourly={HourlyRate}, mileage={MileageRate}, effective={EffectiveDate}",
                    adminId, userId, request.HourlyRate, request.MileageRate, request.EffectiveDate);

                return new UserRateResult
                {
                    Success = true,
                    Message = "Rate set successfully!",
                    Data = new UserRateDto
                    {
                        Id = rate.Id,
                        UserId = rate.UserId,
                        HourlyRate = rate.HourlyRate,
                        MileageRate = rate.MileageRate,
                        EffectiveDate = rate.EffectiveDate,
                        CreatedAt = rate.CreatedAt,
                        CreatedByAdminId = rate.CreatedByAdminId
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting rate for user {UserId}", userId);
                return new UserRateResult
                {
                    Success = false,
                    Message = "Failed to set rate",
                    Errors = [ex.Message]
                };
            }
        }
    }
}
