using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.WeeklyLog;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.WeeklyLog
{
    public class WeeklyLogService(ApplicationDbContext context, ILogger<WeeklyLogService> logger) : IWeeklyLogService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<WeeklyLogService> _logger = logger;

        public async Task<WeeklyLogListResult> GetWeeklyLogsAsync(int userId, PagedRequest request)
        {
            try
            {
                _logger.LogInformation("Retrieving weekly logs for user {UserId} - Page: {Page}, PageSize: {PageSize}",
                    userId, request.Page, request.PageSize);

                var query = _context.WeeklyLogs
                    .Where(w => w.UserId == userId && !w.IsDeleted);

                if (request.StartDate.HasValue)
                {
                    query = query.Where(w => w.DateFrom >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(w => w.DateTo <= request.EndDate.Value);
                }

                var totalCount = await query.CountAsync();

                var weeklyLogs = await query
                    .OrderByDescending(w => w.DateFrom)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(w => new WeeklyLogResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        DateFrom = w.DateFrom,
                        DateTo = w.DateTo,
                        TotalHours = w.TotalHours,
                        TotalCharges = w.TotalCharges,
                        Status = w.Status
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var pagedResult = new PagedResult<WeeklyLogResponse>
                {
                    Data = weeklyLogs,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1
                };

                _logger.LogInformation("Successfully retrieved {Count} weekly logs for user {UserId}",
                    weeklyLogs.Count, userId);

                return new WeeklyLogListResult
                {
                    Success = true,
                    Message = "Weekly logs retrieved successfully!",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weekly logs for user {UserId}", userId);

                return new WeeklyLogListResult
                {
                    Success = false,
                    Message = "Failed to retrieve weekly logs",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<WeeklyLogResult> GetWeeklyLogByIdAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("Retrieving weekly log {WeeklyLogId} for user {UserId}", id, userId);

                var weeklyLog = await _context.WeeklyLogs
                    .Where(w => w.Id == id && w.UserId == userId && !w.IsDeleted)
                    .Select(w => new WeeklyLogResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        DateFrom = w.DateFrom,
                        DateTo = w.DateTo,
                        TotalHours = w.TotalHours,
                        TotalCharges = w.TotalCharges,
                        Status = w.Status
                    })
                    .FirstOrDefaultAsync();

                if (weeklyLog == null)
                {
                    _logger.LogWarning("Weekly log {WeeklyLogId} not found for user {UserId}", id, userId);

                    return new WeeklyLogResult
                    {
                        Success = false,
                        Message = "Weekly log not found",
                        Errors = ["The requested weekly log does not exist or you don't have permission to access it"]
                    };
                }

                _logger.LogInformation("Successfully retrieved weekly log {WeeklyLogId} for user {UserId}", id, userId);

                return new WeeklyLogResult
                {
                    Success = true,
                    Message = "Weekly log retrieved successfully!",
                    Data = weeklyLog
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weekly log {WeeklyLogId} for user {UserId}", id, userId);

                return new WeeklyLogResult
                {
                    Success = false,
                    Message = "Failed to retrieve weekly log",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<WeeklyLogResult> CreateWeeklyLogAsync(WeeklyLogRequest request, int userId)
        {
            try
            {
                _logger.LogInformation("Creating weekly log for user {UserId} from {DateFrom} to {DateTo}",
                    userId, request.DateFrom, request.DateTo);

                // Check for overlapping weekly logs
                var hasOverlap = await _context.WeeklyLogs
                    .AnyAsync(w => w.UserId == userId 
                        && !w.IsDeleted
                        && ((w.DateFrom <= request.DateFrom && w.DateTo >= request.DateFrom)
                            || (w.DateFrom <= request.DateTo && w.DateTo >= request.DateTo)
                            || (w.DateFrom >= request.DateFrom && w.DateTo <= request.DateTo)));

                if (hasOverlap)
                {
                    return new WeeklyLogResult
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["A weekly log already exists for this date range"]
                    };
                }

                var weeklyLog = new Models.Entities.WeeklyLog
                {
                    UserId = userId,
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    TotalHours = 0,
                    TotalCharges = 0,
                    Status = request.Status ?? "Draft",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.WeeklyLogs.Add(weeklyLog);
                await _context.SaveChangesAsync();

                var response = new WeeklyLogResponse
                {
                    Id = weeklyLog.Id,
                    UserId = weeklyLog.UserId,
                    DateFrom = weeklyLog.DateFrom,
                    DateTo = weeklyLog.DateTo,
                    TotalHours = weeklyLog.TotalHours,
                    TotalCharges = weeklyLog.TotalCharges,
                    Status = weeklyLog.Status
                };

                _logger.LogInformation("Successfully created weekly log {WeeklyLogId} for user {UserId}",
                    weeklyLog.Id, userId);

                return new WeeklyLogResult
                {
                    Success = true,
                    Message = "Weekly log created successfully!",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating weekly log for user {UserId}", userId);

                return new WeeklyLogResult
                {
                    Success = false,
                    Message = "Failed to create weekly log",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<WeeklyLogResult> UpdateWeeklyLogAsync(int id, WeeklyLogRequest request, int userId)
        {
            try
            {
                _logger.LogInformation("Updating weekly log {WeeklyLogId} for user {UserId}", id, userId);

                var weeklyLog = await _context.WeeklyLogs
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId && !w.IsDeleted);

                if (weeklyLog == null)
                {
                    _logger.LogWarning("Weekly log {WeeklyLogId} not found for user {UserId}", id, userId);

                    return new WeeklyLogResult
                    {
                        Success = false,
                        Message = "Weekly log not found",
                        Errors = ["The requested weekly log does not exist or you don't have permission to update it"]
                    };
                }

                // Check for overlapping weekly logs (excluding current one)
                var hasOverlap = await _context.WeeklyLogs
                    .AnyAsync(w => w.UserId == userId 
                        && w.Id != id
                        && !w.IsDeleted
                        && ((w.DateFrom <= request.DateFrom && w.DateTo >= request.DateFrom)
                            || (w.DateFrom <= request.DateTo && w.DateTo >= request.DateTo)
                            || (w.DateFrom >= request.DateFrom && w.DateTo <= request.DateTo)));

                if (hasOverlap)
                {
                    return new WeeklyLogResult
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ["Another weekly log already exists for this date range"]
                    };
                }

                weeklyLog.DateFrom = request.DateFrom;
                weeklyLog.DateTo = request.DateTo;
                weeklyLog.Status = request.Status ?? weeklyLog.Status;
                weeklyLog.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new WeeklyLogResponse
                {
                    Id = weeklyLog.Id,
                    UserId = weeklyLog.UserId,
                    DateFrom = weeklyLog.DateFrom,
                    DateTo = weeklyLog.DateTo,
                    TotalHours = weeklyLog.TotalHours,
                    TotalCharges = weeklyLog.TotalCharges,
                    Status = weeklyLog.Status
                };

                _logger.LogInformation("Successfully updated weekly log {WeeklyLogId} for user {UserId}", id, userId);

                return new WeeklyLogResult
                {
                    Success = true,
                    Message = "Weekly log updated successfully!",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating weekly log {WeeklyLogId} for user {UserId}", id, userId);

                return new WeeklyLogResult
                {
                    Success = false,
                    Message = "Failed to update weekly log",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<WeeklyLogResult> DeleteWeeklyLogAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("Deleting weekly log {WeeklyLogId} for user {UserId}", id, userId);

                var weeklyLog = await _context.WeeklyLogs
                    .Include(w => w.DailyLogs)
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId && !w.IsDeleted);

                if (weeklyLog == null)
                {
                    _logger.LogWarning("Weekly log {WeeklyLogId} not found for user {UserId}", id, userId);

                    return new WeeklyLogResult
                    {
                        Success = false,
                        Message = "Weekly log not found",
                        Errors = ["The requested weekly log does not exist or you don't have permission to delete it"]
                    };
                }

                // Soft delete the weeklylog and all associated daily logs
                weeklyLog.IsDeleted = true;
                foreach (var dailyLog in weeklyLog.DailyLogs)
                {
                    dailyLog.IsDeleted = true;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted weekly log {WeeklyLogId} for user {UserId}", id, userId);

                return new WeeklyLogResult
                {
                    Success = true,
                    Message = "Weekly log deleted successfully!",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting weekly log {WeeklyLogId} for user {UserId}", id, userId);

                return new WeeklyLogResult
                {
                    Success = false,
                    Message = "Failed to delete weekly log",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task RecalculateWeeklyTotalsAsync(int weeklyLogId)
        {
            try
            {
                _logger.LogInformation("Recalculating totals for weekly log {WeeklyLogId}", weeklyLogId);

                var weeklyLog = await _context.WeeklyLogs
                    .Include(w => w.DailyLogs.Where(d => !d.IsDeleted))
                    .FirstOrDefaultAsync(w => w.Id == weeklyLogId && !w.IsDeleted);

                if (weeklyLog == null)
                {
                    _logger.LogWarning("Weekly log {WeeklyLogId} not found for recalculation", weeklyLogId);
                    return;
                }

                weeklyLog.TotalHours = weeklyLog.DailyLogs.Sum(d => d.Hours);
                weeklyLog.TotalCharges = weeklyLog.DailyLogs.Sum(d => d.TollCharge + d.ParkingFee + d.OtherCharges);
                weeklyLog.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully recalculated totals for weekly log {WeeklyLogId}: Hours={TotalHours}, Charges={TotalCharges}",
                    weeklyLogId, weeklyLog.TotalHours, weeklyLog.TotalCharges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating totals for weekly log {WeeklyLogId}", weeklyLogId);
                throw;
            }
        }

        public async Task<bool> WeeklyLogExistsAsync(int weeklyLogId, int userId)
        {
            return await _context.WeeklyLogs
                .AnyAsync(w => w.Id == weeklyLogId && w.UserId == userId && !w.IsDeleted);
        }
    }
}