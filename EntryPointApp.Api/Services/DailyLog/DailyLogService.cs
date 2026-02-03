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

        public async Task<DailyLogListResult> CreateDailyLogsBatchAsync(int weeklyLogId, List<DailyLogRequest> requests, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Creating {Count} dailylogs for weeklylog {WeeklyLogId} and user {UserId}",
                    requests.Count, weeklyLogId, userId);

                var weeklyLog = await _context.WeeklyLogs
                    .FirstOrDefaultAsync(w => w.Id == weeklyLogId && w.UserId == userId && !w.IsDeleted);

                if (weeklyLog == null)
                {
                    return new DailyLogListResult
                    {
                        Success = false,
                        Message = "Weeklylog not found",
                        Errors = ["The requested weeklylog does not exist or you don't have permission to access it"]
                    };
                }

                var responses = new List<DailyLogResponse>();
                var errors = new List<string>();

                foreach (var request in requests)
                {
                    if (request.Date < weeklyLog.DateFrom || request.Date > weeklyLog.DateTo)
                    {
                        errors.Add($"Date {request.Date} is outside the weeklylog date range ({weeklyLog.DateFrom} to {weeklyLog.DateTo})");
                        continue;
                    }

                    var duplicateExists = await _context.DailyLogs
                        .AnyAsync(d => d.WeeklyLogId == weeklyLogId && d.Date == request.Date && !d.IsDeleted);

                    if (duplicateExists)
                    {
                        errors.Add($"A daily log already exists for date {request.Date}");
                        continue;
                    }

                    if (requests.Count(r => r.Date == request.Date) > 1)
                    {
                        errors.Add($"Duplicate date {request.Date} in request");
                        continue;
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

                    responses.Add(new DailyLogResponse
                    {
                        Id = dailyLog.Id,
                        Date = dailyLog.Date,
                        Hours = dailyLog.Hours,
                        Mileage = dailyLog.Mileage,
                        TollCharge = dailyLog.TollCharge,
                        ParkingFee = dailyLog.ParkingFee,
                        OtherCharges = dailyLog.OtherCharges,
                        Comment = dailyLog.Comment
                    });
                }

                if (errors.Any())
                {
                    await transaction.RollbackAsync();
                    return new DailyLogListResult
                    {
                        Success = false,
                        Message = "Some daily logs could not be created",
                        Errors = errors
                    };
                }

                await _weeklyLogService.RecalculateWeeklyTotalsAsync(weeklyLogId);
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully created {Count} dailylogs for weeklylog {WeeklyLogId}",
                    responses.Count, weeklyLogId);

                return new DailyLogListResult
                {
                    Success = true,
                    Message = $"{responses.Count} daily log(s) created successfully!",
                    Data = responses
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating daily logs batch for weeklylog {WeeklyLogId}", weeklyLogId);

                return new DailyLogListResult
                {
                    Success = false,
                    Message = "Failed to create daily logs",
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

        public async Task<DailyLogListResult> UpdateDailyLogsAsync(int weeklyLogId, UpdateDailyLogsRequest request, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Updating daily logs for weeklylog {WeeklyLogId} and user {UserId}",
                    weeklyLogId, userId);

                var weeklyLog = await _context.WeeklyLogs
                    .FirstOrDefaultAsync(w => w.Id == weeklyLogId && w.UserId == userId && !w.IsDeleted);

                if (weeklyLog == null)
                {
                    return new DailyLogListResult
                    {
                        Success = false,
                        Message = "Weeklylog not found",
                        Errors = ["The requested weeklylog does not exist or you don't have permission to access it"]
                    };
                }

                if (weeklyLog.Status != Models.Enums.TimesheetStatus.Draft)
                {
                    return new DailyLogListResult
                    {
                        Success = false,
                        Message = "Cannot edit timesheet",
                        Errors = [$"Only timesheets with Draft status can be edited. Current status: {weeklyLog.Status}"]
                    };
                }

                var existingDailyLogs = await _context.DailyLogs
                    .Where(d => d.WeeklyLogId == weeklyLogId && !d.IsDeleted)
                    .ToListAsync();

                var responses = new List<DailyLogResponse>();
                var errors = new List<string>();
                var processedIds = new HashSet<int>();

                foreach (var item in request.DailyLogs)
                {
                    if (item.Date < weeklyLog.DateFrom || item.Date > weeklyLog.DateTo)
                    {
                        errors.Add($"Date {item.Date} is outside the weeklylog date range ({weeklyLog.DateFrom} to {weeklyLog.DateTo})");
                        continue;
                    }

                    var duplicateInRequest = request.DailyLogs
                        .Count(d => d.Date == item.Date) > 1;
                    
                    if (duplicateInRequest)
                    {
                        errors.Add($"Duplicate date {item.Date} in request");
                        continue;
                    }

                    if (item.Id.HasValue && item.Id.Value > 0)
                    {
                        var existingLog = existingDailyLogs.FirstOrDefault(d => d.Id == item.Id.Value);
                        
                        if (existingLog == null)
                        {
                            errors.Add($"DailyLog with ID {item.Id} not found");
                            continue;
                        }

                        if (existingLog.Date != item.Date)
                        {
                            var dateConflict = existingDailyLogs
                                .Any(d => d.Date == item.Date && d.Id != item.Id.Value);
                            
                            if (dateConflict)
                            {
                                errors.Add($"Another daily log already exists for date {item.Date}");
                                continue;
                            }
                        }

                        existingLog.Date = item.Date;
                        existingLog.Hours = item.Hours;
                        existingLog.Mileage = item.Mileage;
                        existingLog.TollCharge = item.TollCharge;
                        existingLog.ParkingFee = item.ParkingFee;
                        existingLog.OtherCharges = item.OtherCharges;
                        existingLog.Comment = item.Comment ?? string.Empty;
                        existingLog.UpdatedAt = DateTime.UtcNow;

                        processedIds.Add(existingLog.Id);

                        responses.Add(new DailyLogResponse
                        {
                            Id = existingLog.Id,
                            Date = existingLog.Date,
                            Hours = existingLog.Hours,
                            Mileage = existingLog.Mileage,
                            TollCharge = existingLog.TollCharge,
                            ParkingFee = existingLog.ParkingFee,
                            OtherCharges = existingLog.OtherCharges,
                            Comment = existingLog.Comment
                        });
                    }
                    else
                    {
                        var duplicateExists = existingDailyLogs
                            .Any(d => d.Date == item.Date);

                        if (duplicateExists)
                        {
                            errors.Add($"A daily log already exists for date {item.Date}");
                            continue;
                        }

                        var newLog = new Models.Entities.DailyLog
                        {
                            UserId = userId,
                            WeeklyLogId = weeklyLogId,
                            Date = item.Date,
                            Hours = item.Hours,
                            Mileage = item.Mileage,
                            TollCharge = item.TollCharge,
                            ParkingFee = item.ParkingFee,
                            OtherCharges = item.OtherCharges,
                            Comment = item.Comment ?? string.Empty,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.DailyLogs.Add(newLog);
                        await _context.SaveChangesAsync();

                        processedIds.Add(newLog.Id);

                        responses.Add(new DailyLogResponse
                        {
                            Id = newLog.Id,
                            Date = newLog.Date,
                            Hours = newLog.Hours,
                            Mileage = newLog.Mileage,
                            TollCharge = newLog.TollCharge,
                            ParkingFee = newLog.ParkingFee,
                            OtherCharges = newLog.OtherCharges,
                            Comment = newLog.Comment
                        });
                    }
                }

                var logsToDelete = existingDailyLogs
                    .Where(d => !processedIds.Contains(d.Id))
                    .ToList();

                foreach (var log in logsToDelete)
                {
                    log.IsDeleted = true;
                    log.UpdatedAt = DateTime.UtcNow;
                }

                if (errors.Any())
                {
                    await transaction.RollbackAsync();
                    return new DailyLogListResult
                    {
                        Success = false,
                        Message = "Some daily logs could not be updated",
                        Errors = errors
                    };
                }

                await _context.SaveChangesAsync();
                await _weeklyLogService.RecalculateWeeklyTotalsAsync(weeklyLogId);
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully updated daily logs for weeklylog {WeeklyLogId}. Created/Updated: {Count}, Deleted: {DeletedCount}",
                    weeklyLogId, responses.Count, logsToDelete.Count);

                return new DailyLogListResult
                {
                    Success = true,
                    Message = $"Daily logs updated successfully! {responses.Count} active, {logsToDelete.Count} removed.",
                    Data = responses
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating daily logs for weeklylog {WeeklyLogId}", weeklyLogId);

                return new DailyLogListResult
                {
                    Success = false,
                    Message = "Failed to update daily logs",
                    Errors = [ex.Message]
                };
            }
        }
    }
}