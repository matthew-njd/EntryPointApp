using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;
using EntryPointApp.Api.Models.Dtos.DailyLog;
using EntryPointApp.Api.Services.WeeklyLog;
using EntryPointApp.Api.Services.DailyLog;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.Timesheet
{
    /// <summary>
    /// TimesheetService refactored for WeeklyLogService and DailyLogService
    /// Keeps existing endpoints working while enabling the new granular approach
    /// </summary>
    public class TimesheetService(
        ApplicationDbContext context,
        IWeeklyLogService weeklyLogService,
        IDailyLogService dailyLogService,
        ILogger<TimesheetService> logger) : ITimesheetService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IWeeklyLogService _weeklyLogService = weeklyLogService;
        private readonly IDailyLogService _dailyLogService = dailyLogService;
        private readonly ILogger<TimesheetService> _logger = logger;

        public async Task<TimesheetListResult> GetTimesheetsAsync(int userId, PagedRequest request)
        {
            try
            {
                _logger.LogInformation("Retrieving complete timesheets for user {UserId}", userId);

                var query = _context.WeeklyLogs
                    .Include(w => w.DailyLogs.Where(d => !d.IsDeleted))
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

                var timesheets = await query
                    .OrderByDescending(w => w.DateFrom)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(w => new TimesheetResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        DateFrom = w.DateFrom,
                        DateTo = w.DateTo,
                        TotalHours = w.TotalHours,
                        TotalCharges = w.TotalCharges,
                        Status = w.Status,
                        DailyLogs = w.DailyLogs.Select(d => new DailyLogResponse
                        {
                            Id = d.Id,
                            Date = d.Date,
                            Hours = d.Hours,
                            Mileage = d.Mileage,
                            TollCharge = d.TollCharge,
                            ParkingFee = d.ParkingFee,
                            OtherCharges = d.OtherCharges,
                            Comment = d.Comment
                        }).ToList()
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var pagedResult = new PagedResult<TimesheetResponse>
                {
                    Data = timesheets,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1
                };

                _logger.LogInformation("Successfully retrieved {Count} complete timesheets for user {UserId}",
                    timesheets.Count, userId);

                return new TimesheetListResult
                {
                    Success = true,
                    Message = "Timesheets retrieved successfully!",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving complete timesheets for user {UserId}", userId);

                return new TimesheetListResult
                {
                    Success = false,
                    Message = "Failed to retrieve timesheets",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<TimesheetListWithSummaryResult> GetTimesheetsWithSummaryAsync(int userId, PagedRequest request)
        {
            try
            {
                _logger.LogInformation("Retrieving timesheets with summary for user {UserId}", userId);

                var query = _context.WeeklyLogs
                    .Include(w => w.DailyLogs.Where(d => !d.IsDeleted))
                    .Where(w => w.UserId == userId && !w.IsDeleted);

                if (request.StartDate.HasValue)
                {
                    query = query.Where(w => w.DateFrom >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(w => w.DateTo <= request.EndDate.Value);
                }

                var summaryData = await query
                    .GroupBy(w => 1)
                    .Select(g => new TimesheetSummaryResponse
                    {
                        TotalHours = g.Sum(w => w.TotalHours),
                        TotalMileage = g.SelectMany(w => w.DailyLogs).Sum(d => d.Mileage),
                        TotalExpenses = g.SelectMany(w => w.DailyLogs).Sum(d => d.TollCharge + d.ParkingFee + d.OtherCharges),
                        TimesheetCount = g.Count()
                    })
                    .FirstOrDefaultAsync() ?? new TimesheetSummaryResponse();

                var totalCount = await query.CountAsync();

                var timesheets = await query
                    .OrderByDescending(w => w.DateFrom)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(w => new TimesheetResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        DateFrom = w.DateFrom,
                        DateTo = w.DateTo,
                        TotalHours = w.TotalHours,
                        TotalCharges = w.TotalCharges,
                        Status = w.Status,
                        DailyLogs = w.DailyLogs.Select(d => new DailyLogResponse
                        {
                            Id = d.Id,
                            Date = d.Date,
                            Hours = d.Hours,
                            Mileage = d.Mileage,
                            TollCharge = d.TollCharge,
                            ParkingFee = d.ParkingFee,
                            OtherCharges = d.OtherCharges,
                            Comment = d.Comment
                        }).ToList()
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var pagedResult = new PagedResult<TimesheetResponse>
                {
                    Data = timesheets,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1
                };

                _logger.LogInformation("Successfully retrieved {Count} timesheets with summary for user {UserId}",
                    timesheets.Count, userId);

                return new TimesheetListWithSummaryResult
                {
                    Success = true,
                    Message = "Timesheets with summary retrieved successfully!",
                    Data = pagedResult,
                    Summary = summaryData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving timesheets with summary for user {UserId}", userId);

                return new TimesheetListWithSummaryResult
                {
                    Success = false,
                    Message = "Failed to retrieve timesheets with summary",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<TimesheetResult> GetTimesheetByIdAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("Retrieving complete timesheet {TimesheetId} for user {UserId}", id, userId);

                var timesheet = await _context.WeeklyLogs
                    .Include(w => w.DailyLogs.Where(d => !d.IsDeleted))
                    .Where(w => w.Id == id && w.UserId == userId && !w.IsDeleted)
                    .Select(w => new TimesheetResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        DateFrom = w.DateFrom,
                        DateTo = w.DateTo,
                        TotalHours = w.TotalHours,
                        TotalCharges = w.TotalCharges,
                        Status = w.Status,
                        DailyLogs = w.DailyLogs.Select(d => new DailyLogResponse
                        {
                            Id = d.Id,
                            Date = d.Date,
                            Hours = d.Hours,
                            Mileage = d.Mileage,
                            TollCharge = d.TollCharge,
                            ParkingFee = d.ParkingFee,
                            OtherCharges = d.OtherCharges,
                            Comment = d.Comment
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (timesheet == null)
                {
                    _logger.LogWarning("Timesheet {TimesheetId} not found for user {UserId}", id, userId);

                    return new TimesheetResult
                    {
                        Success = false,
                        Message = "Timesheet not found",
                        Errors = ["The requested timesheet does not exist or you don't have permission to access it"]
                    };
                }

                _logger.LogInformation("Successfully retrieved complete timesheet {TimesheetId}", id);

                return new TimesheetResult
                {
                    Success = true,
                    Message = "Timesheet retrieved successfully!",
                    Data = timesheet
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving timesheet {TimesheetId}", id);

                return new TimesheetResult
                {
                    Success = false,
                    Message = "Failed to retrieve timesheet",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<TimesheetResult> CreateTimesheetAsync(TimesheetRequest request, int userId)
        {
            try
            {
                _logger.LogInformation("Creating complete timesheet for user {UserId}", userId);

                // Use WeeklyLogService to create the week
                var weeklyLogRequest = new Models.Dtos.WeeklyLog.WeeklyLogRequest
                {
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    Status = request.Status
                };

                var weeklyLogResult = await _weeklyLogService.CreateWeeklyLogAsync(weeklyLogRequest, userId);

                if (!weeklyLogResult.Success)
                {
                    return new TimesheetResult
                    {
                        Success = false,
                        Message = weeklyLogResult.Message,
                        Errors = weeklyLogResult.Errors
                    };
                }

                var weeklyLogId = weeklyLogResult.Data!.Id;

                // Use DailyLogService to create each dailylog
                if (request.DailyLogs != null && request.DailyLogs.Any())
                {
                    foreach (var dailyLogReq in request.DailyLogs)
                    {
                        var dailyLogRequest = new DailyLogRequest
                        {
                            Date = dailyLogReq.Date,
                            Hours = dailyLogReq.Hours,
                            Mileage = dailyLogReq.Mileage,
                            TollCharge = dailyLogReq.TollCharge,
                            ParkingFee = dailyLogReq.ParkingFee,
                            OtherCharges = dailyLogReq.OtherCharges,
                            Comment = dailyLogReq.Comment
                        };

                        var dailyLogResult = await _dailyLogService.CreateDailyLogAsync(weeklyLogId, dailyLogRequest, userId);

                        if (!dailyLogResult.Success)
                        {
                            // Rollback: delete the weeklylog if any dailylog fails
                            await _weeklyLogService.DeleteWeeklyLogAsync(weeklyLogId, userId);

                            return new TimesheetResult
                            {
                                Success = false,
                                Message = "Failed to create dailylog",
                                Errors = dailyLogResult.Errors
                            };
                        }
                    }
                }

                // Fetch the complete timesheet
                var result = await GetTimesheetByIdAsync(weeklyLogId, userId);

                _logger.LogInformation("Successfully created complete timesheet {TimesheetId}", weeklyLogId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating complete timesheet for user {UserId}", userId);

                return new TimesheetResult
                {
                    Success = false,
                    Message = "Failed to create timesheet",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<TimesheetResult> UpdateTimesheetAsync(int id, TimesheetRequest request, int userId)
        {
            try
            {
                _logger.LogInformation("Updating complete timesheet {TimesheetId}", id);

                // Update weeklylog
                var weeklyLogRequest = new Models.Dtos.WeeklyLog.WeeklyLogRequest
                {
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    Status = request.Status
                };

                var weeklyLogResult = await _weeklyLogService.UpdateWeeklyLogAsync(id, weeklyLogRequest, userId);

                if (!weeklyLogResult.Success)
                {
                    return new TimesheetResult
                    {
                        Success = false,
                        Message = weeklyLogResult.Message,
                        Errors = weeklyLogResult.Errors
                    };
                }

                // Delete all existing dailylogs
                var existingDailyLogs = await _context.DailyLogs
                    .Where(d => d.WeeklyLogId == id && !d.IsDeleted)
                    .ToListAsync();

                foreach (var dailyLog in existingDailyLogs)
                {
                    await _dailyLogService.DeleteDailyLogAsync(dailyLog.Id, id, userId);
                }

                // Create new dailylogs
                if (request.DailyLogs != null && request.DailyLogs.Any())
                {
                    foreach (var dailyLogReq in request.DailyLogs)
                    {
                        var dailyLogRequest = new DailyLogRequest
                        {
                            Date = dailyLogReq.Date,
                            Hours = dailyLogReq.Hours,
                            Mileage = dailyLogReq.Mileage,
                            TollCharge = dailyLogReq.TollCharge,
                            ParkingFee = dailyLogReq.ParkingFee,
                            OtherCharges = dailyLogReq.OtherCharges,
                            Comment = dailyLogReq.Comment
                        };

                        var dailyLogResult = await _dailyLogService.CreateDailyLogAsync(id, dailyLogRequest, userId);

                        if (!dailyLogResult.Success)
                        {
                            return new TimesheetResult
                            {
                                Success = false,
                                Message = "Failed to update dailylog",
                                Errors = dailyLogResult.Errors
                            };
                        }
                    }
                }

                // Fetch the updated timesheet
                var result = await GetTimesheetByIdAsync(id, userId);

                _logger.LogInformation("Successfully updated complete timesheet {TimesheetId}", id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timesheet {TimesheetId}", id);

                return new TimesheetResult
                {
                    Success = false,
                    Message = "Failed to update timesheet",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<TimesheetResult> DeleteTimesheetAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("Deleting complete timesheet {TimesheetId}", id);

                // WeeklyLogService handles cascading delete of dailylogs
                var result = await _weeklyLogService.DeleteWeeklyLogAsync(id, userId);

                return new TimesheetResult
                {
                    Success = result.Success,
                    Message = result.Message,
                    Errors = result.Errors,
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting timesheet {TimesheetId}", id);

                return new TimesheetResult
                {
                    Success = false,
                    Message = "Failed to delete timesheet",
                    Errors = [ex.Message]
                };
            }
        }
    }
}