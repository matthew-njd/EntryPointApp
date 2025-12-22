using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.DailyLog;
using EntryPointApp.Api.Services.WeeklyLog;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.DailyLog
{
    public class DailyLogService(ApplicationDbContext context, IWeeklyLogService weeklyLogService, ILogger<DailyLogService> logger) : IDailyLogService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IWeeklyLogService _weeklyLogService = weeklyLogService;
        private readonly ILogger<DailyLogService> _logger = logger;

        public async Task<DailyLogListResult> GetDailyLogsByWeeklyLogIdAsync(int weeklyLogId, int userId)
        {
            try
            {
                _logger.LogInformation("Retrieving dailylogs for weeklylog {WeeklyLogId} and user {UserId}",
                    weeklyLogId, userId);

                var weeklyLogExists = await _weeklyLogService.WeeklyLogExistsAsync(weeklyLogId, userId);
                if (!weeklyLogExists)
                {
                    return new DailyLogListResult
                    {
                        Success = false,
                        Message = " Weeklylog not found",
                        Errors = ["The requested weeklylog does not exist or you don't have permission to access it"]
                    };
                }

                var dailyLogs = await _context.DailyLogs
                    .Where(d => d.WeeklyLogId == weeklyLogId && d.UserId == userId && !d.IsDeleted)
                    .OrderBy(d => d.Date)
                    .Select(d => new DailyLogResponse
                    {
                        Id = d.Id,
                        Date = d.Date,
                        Hours = d.Hours,
                        Mileage = d.Mileage,
                        TollCharge = d.TollCharge,
                        ParkingFee = d.ParkingFee,
                        OtherCharges = d.OtherCharges,
                        Comment = d.Comment
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} dailylogs for weeklylog {WeeklyLogId}",
                    dailyLogs.Count, weeklyLogId);

                return new DailyLogListResult
                {
                    Success = true,
                    Message = "Dailylogs retrieved successfully!",
                    Data = dailyLogs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dailylogs for weeklylog {WeeklyLogId}", weeklyLogId);

                return new DailyLogListResult
                {
                    Success = false,
                    Message = "Failed to retrieve dailylogs",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<DailyLogResult> GetDailyLogByIdAsync(int id, int weeklyLogId, int userId)
        {
            try
            {
                _logger.LogInformation("Retrieving dailylog {DailyLogId} for weeklylog {WeeklyLogId} and user {UserId}",
                    id, weeklyLogId, userId);

                var dailyLog = await _context.DailyLogs
                    .Where(d => d.Id == id
                        && d.WeeklyLogId == weeklyLogId
                        && d.UserId == userId
                        && !d.IsDeleted)
                    .Select(d => new DailyLogResponse
                    {
                        Id = d.Id,
                        Date = d.Date,
                        Hours = d.Hours,
                        Mileage = d.Mileage,
                        TollCharge = d.TollCharge,
                        ParkingFee = d.ParkingFee,
                        OtherCharges = d.OtherCharges,
                        Comment = d.Comment
                    })
                    .FirstOrDefaultAsync();

                if (dailyLog == null)
                {
                    _logger.LogWarning("Dailylog {DailyLogId} not found for weeklylog {WeeklyLogId}",
                        id, weeklyLogId);

                    return new DailyLogResult
                    {
                        Success = false,
                        Message = "Dailylog not found",
                        Errors = ["The requested dailylog does not exist or you don't have permission to access it"]
                    };
                }

                _logger.LogInformation("Successfully retrieved dailylog {DailyLogId}", id);

                return new DailyLogResult
                {
                    Success = true,
                    Message = "Dailylog retrieved successfully!",
                    Data = dailyLog
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dailylog {DailyLogId}", id);

                return new DailyLogResult
                {
                    Success = false,
                    Message = "Failed to retrieve dailylog",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<DailyLogResult> CreateDailyLogAsync(int weeklyLogId, DailyLogRequest request, int userId)
        {
            try
            {
                _logger.LogInformation("Creating dailylog for weeklylog {WeeklyLogId} and user {UserId}",
                    weeklyLogId, userId);

                var weeklyLog = await _context.WeeklyLogs
                    .FirstOrDefaultAsync(w => w.Id == weeklyLogId && w.UserId == userId && !w.IsDeleted);

                if (weeklyLog == null)
                {
                    return new DailyLogResult
                    {
                        Success = false,
                        Message = " Weeklylog not found",
                        Errors = ["The requested weeklylog does not exist or you don't have permission to access it"]
                    };
                }

                if (request.Date < weeklyLog.DateFrom || request.Date > weeklyLog.DateTo)
                {
                    return new DailyLogResult
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = [$"Date {request.Date} is outside the weeklylog date range ({weeklyLog.DateFrom} to {weeklyLog.DateTo})"]
                    };
                }

                var duplicateExists = await _context.DailyLogs
                    .AnyAsync(d => d.WeeklyLogId == weeklyLogId
                        && d.Date == request.Date
                        && !d.IsDeleted);

                if (duplicateExists)
                {
                    return new DailyLogResult
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = [$"A daily log already exists for date {request.Date} in this weeklylog"]
                    };
                }

                var dailyLog = new Models.Entities.DailyLog
                {
                    UserId = userId,
                    WeeklyLogId = weeklyLogId,
                    Date = request.Date,
                    Hours = request.Hours,
                    Mileage = request.Mileage,
                    TollCharge = request.TollCharge,
                    ParkingFee = request.ParkingFee,
                    OtherCharges = request.OtherCharges,
                    Comment = request.Comment ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DailyLogs.Add(dailyLog);
                await _context.SaveChangesAsync();

                await _weeklyLogService.RecalculateWeeklyTotalsAsync(weeklyLogId);

                var response = new DailyLogResponse
                {
                    Id = dailyLog.Id,
                    Date = dailyLog.Date,
                    Hours = dailyLog.Hours,
                    Mileage = dailyLog.Mileage,
                    TollCharge = dailyLog.TollCharge,
                    ParkingFee = dailyLog.ParkingFee,
                    OtherCharges = dailyLog.OtherCharges,
                    Comment = dailyLog.Comment
                };

                _logger.LogInformation("Successfully created dailylog {DailyLogId} for weeklylog {WeeklyLogId}",
                    dailyLog.Id, weeklyLogId);

                return new DailyLogResult
                {
                    Success = true,
                    Message = "Dailylog created successfully!",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dailylog for weeklylog {WeeklyLogId}", weeklyLogId);

                return new DailyLogResult
                {
                    Success = false,
                    Message = "Failed to create daily log",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<DailyLogResult> UpdateDailyLogAsync(int id, int weeklyLogId, DailyLogRequest request, int userId)
        {
            try
            {
                _logger.LogInformation("Updating dailylog {DailyLogId} for weeklylog {WeeklyLogId}",
                    id, weeklyLogId);

                var dailyLog = await _context.DailyLogs
                    .FirstOrDefaultAsync(d => d.Id == id
                        && d.WeeklyLogId == weeklyLogId
                        && d.UserId == userId
                        && !d.IsDeleted);

                if (dailyLog == null)
                {
                    _logger.LogWarning("Dailylog {DailyLogId} not found for weeklylog {WeeklyLogId}",
                        id, weeklyLogId);

                    return new DailyLogResult
                    {
                        Success = false,
                        Message = "Dailylog not found",
                        Errors = ["The requested dailylog does not exist or you don't have permission to update it"]
                    };
                }

                var weeklyLog = await _context.WeeklyLogs
                    .FirstOrDefaultAsync(w => w.Id == weeklyLogId && !w.IsDeleted);

                if (weeklyLog == null)
                {
                    return new DailyLogResult
                    {
                        Success = false,
                        Message = "Weeklylog not found",
                        Errors = ["The associated weeklylog no longer exists"]
                    };
                }

                if (request.Date < weeklyLog.DateFrom || request.Date > weeklyLog.DateTo)
                {
                    return new DailyLogResult
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = [$"Date {request.Date} is outside the weeklylog date range ({weeklyLog.DateFrom} to {weeklyLog.DateTo})"]
                    };
                }

                var duplicateExists = await _context.DailyLogs
                    .AnyAsync(d => d.WeeklyLogId == weeklyLogId
                        && d.Date == request.Date
                        && d.Id != id
                        && !d.IsDeleted);

                if (duplicateExists)
                {
                    return new DailyLogResult
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = [$"Another dailylog already exists for date {request.Date} in this  weeklylog"]
                    };
                }

                dailyLog.Date = request.Date;
                dailyLog.Hours = request.Hours;
                dailyLog.Mileage = request.Mileage;
                dailyLog.TollCharge = request.TollCharge;
                dailyLog.ParkingFee = request.ParkingFee;
                dailyLog.OtherCharges = request.OtherCharges;
                dailyLog.Comment = request.Comment ?? string.Empty;
                dailyLog.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _weeklyLogService.RecalculateWeeklyTotalsAsync(weeklyLogId);

                var response = new DailyLogResponse
                {
                    Id = dailyLog.Id,
                    Date = dailyLog.Date,
                    Hours = dailyLog.Hours,
                    Mileage = dailyLog.Mileage,
                    TollCharge = dailyLog.TollCharge,
                    ParkingFee = dailyLog.ParkingFee,
                    OtherCharges = dailyLog.OtherCharges,
                    Comment = dailyLog.Comment
                };

                _logger.LogInformation("Successfully updated dailylog {DailyLogId}", id);

                return new DailyLogResult
                {
                    Success = true,
                    Message = "Dailylog updated successfully!",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dailylog {DailyLogId}", id);

                return new DailyLogResult
                {
                    Success = false,
                    Message = "Failed to update daily log",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<DailyLogResult> DeleteDailyLogAsync(int id, int weeklyLogId, int userId)
        {
            try
            {
                _logger.LogInformation("Deleting dailylog {DailyLogId} for weeklylog {WeeklyLogId}",
                    id, weeklyLogId);

                var dailyLog = await _context.DailyLogs
                    .FirstOrDefaultAsync(d => d.Id == id
                        && d.WeeklyLogId == weeklyLogId
                        && d.UserId == userId
                        && !d.IsDeleted);

                if (dailyLog == null)
                {
                    _logger.LogWarning("Dailylog {DailyLogId} not found for weeklylog {WeeklyLogId}",
                        id, weeklyLogId);

                    return new DailyLogResult
                    {
                        Success = false,
                        Message = "Dailylog not found",
                        Errors = ["The requested dailylog does not exist or you don't have permission to delete it"]
                    };
                }

                dailyLog.IsDeleted = true;
                await _context.SaveChangesAsync();

                await _weeklyLogService.RecalculateWeeklyTotalsAsync(weeklyLogId);

                _logger.LogInformation("Successfully deleted dailylog {DailyLogId}", id);

                return new DailyLogResult
                {
                    Success = true,
                    Message = "Dailylog deleted successfully!",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting daily log {DailyLogId}", id);

                return new DailyLogResult
                {
                    Success = false,
                    Message = "Failed to delete dailylog",
                    Errors = [ex.Message]
                };
            }
        }
    }
}