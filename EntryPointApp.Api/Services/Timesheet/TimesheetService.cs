using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;
using EntryPointApp.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.Timesheet
{
    public class TimesheetService(ApplicationDbContext context, ILogger<TimesheetService> logger) : ITimesheetService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<TimesheetService> _logger = logger;

        public async Task<TimesheetListResult> GetTimesheetsAsync(int userId, PagedRequest request)
        {
            try
            {
                _logger.LogInformation("Retrieving timesheets for user {UserId} - Page: {Page}, PageSize: {PageSize}",
                    userId, request.Page, request.PageSize);

                var query = _context.WeeklyLogs
                    .Include(w => w.DailyLogs)
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
                        TollCharges = w.TollCharges,
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

                _logger.LogInformation("Successfully retrieved {Count} timesheets for user {UserId}", 
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
                _logger.LogError(ex, "Error retrieving timesheets for user {UserId}", userId);

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
                _logger.LogInformation("Retrieving timesheets with summary for user {UserId} - Page: {Page}, PageSize: {PageSize}",
                    userId, request.Page, request.PageSize);

                var query = _context.WeeklyLogs
                    .Include(w => w.DailyLogs)
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
                        TollCharges = w.TollCharges,
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
                _logger.LogInformation("Retrieving timesheet {TimesheetId} for user {UserId}", id, userId);

                var timesheet = await _context.WeeklyLogs
                    .Include(w => w.DailyLogs)
                    .Where(w => w.Id == id && w.UserId == userId && !w.IsDeleted)
                    .Select(w => new TimesheetResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        DateFrom = w.DateFrom,
                        DateTo = w.DateTo,
                        TotalHours = w.TotalHours,
                        TollCharges = w.TollCharges,
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

                _logger.LogInformation("Successfully retrieved timesheet {TimesheetId} for user {UserId}", id, userId);

                return new TimesheetResult
                {
                    Success = true,
                    Message = "Timesheet retrieved successfully!",
                    Data = timesheet
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving timesheet {TimesheetId} for user {UserId}", id, userId);

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
                _logger.LogInformation("Creating timesheet for user {UserId} from {DateFrom} to {DateTo}", 
                    userId, request.DateFrom, request.DateTo);

                // Create the weekly log
                var weeklyLog = new WeeklyLog
                {
                    UserId = userId,
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    TotalHours = request.DailyLogs?.Sum(d => d.Hours) ?? 0,
                    TollCharges = request.DailyLogs?.Sum(d => d.TollCharge) ?? 0,
                    Status = request.Status ?? "Draft",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                _context.WeeklyLogs.Add(weeklyLog);
                await _context.SaveChangesAsync();

                // Create daily logs if provided
                if (request.DailyLogs != null && request.DailyLogs.Any())
                {
                    var dailyLogs = request.DailyLogs.Select(d => new DailyLog
                    {
                        UserId = userId,
                        WeeklyLogId = weeklyLog.Id,
                        Date = d.Date,
                        Hours = d.Hours,
                        Mileage = d.Mileage,
                        TollCharge = d.TollCharge,
                        ParkingFee = d.ParkingFee,
                        OtherCharges = d.OtherCharges,
                        Comment = d.Comment ?? string.Empty,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList();

                    _context.DailyLogs.AddRange(dailyLogs);
                    await _context.SaveChangesAsync();
                }

                // Fetch the complete timesheet with daily logs
                var createdTimesheet = await _context.WeeklyLogs
                    .Include(w => w.DailyLogs)
                    .Where(w => w.Id == weeklyLog.Id)
                    .Select(w => new TimesheetResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        DateFrom = w.DateFrom,
                        DateTo = w.DateTo,
                        TotalHours = w.TotalHours,
                        TollCharges = w.TollCharges,
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
                    .FirstAsync();

                _logger.LogInformation("Successfully created timesheet {TimesheetId} for user {UserId}", 
                    weeklyLog.Id, userId);

                return new TimesheetResult
                {
                    Success = true,
                    Message = "Timesheet created successfully!",
                    Data = createdTimesheet
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating timesheet for user {UserId}", userId);
                
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
                _logger.LogInformation("Updating timesheet {TimesheetId} for user {UserId}", id, userId);

                var existingTimesheet = await _context.WeeklyLogs
                    .Include(w => w.DailyLogs)
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId && !w.IsDeleted);

                if (existingTimesheet == null)
                {
                    _logger.LogWarning("Timesheet {TimesheetId} not found for user {UserId} during update", id, userId);
                    
                    return new TimesheetResult
                    {
                        Success = false,
                        Message = "Timesheet not found",
                        Errors = ["The requested timesheet does not exist or you don't have permission to update it"]
                    };
                }

                // Update weekly log properties
                existingTimesheet.DateFrom = request.DateFrom;
                existingTimesheet.DateTo = request.DateTo;
                existingTimesheet.Status = request.Status ?? existingTimesheet.Status;
                existingTimesheet.UpdatedAt = DateTime.UtcNow;

                // Update or create daily logs
                if (request.DailyLogs != null)
                {
                    // Remove existing daily logs
                    _context.DailyLogs.RemoveRange(existingTimesheet.DailyLogs);

                    // Add new daily logs
                    var dailyLogs = request.DailyLogs.Select(d => new DailyLog
                    {
                        UserId = userId,
                        WeeklyLogId = existingTimesheet.Id,
                        Date = d.Date,
                        Hours = d.Hours,
                        Mileage = d.Mileage,
                        TollCharge = d.TollCharge,
                        ParkingFee = d.ParkingFee,
                        OtherCharges = d.OtherCharges,
                        Comment = d.Comment ?? string.Empty,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList();

                    _context.DailyLogs.AddRange(dailyLogs);

                    // Update totals
                    existingTimesheet.TotalHours = dailyLogs.Sum(d => d.Hours);
                    existingTimesheet.TollCharges = dailyLogs.Sum(d => d.TollCharge);
                }

                await _context.SaveChangesAsync();

                // Fetch updated timesheet
                var updatedTimesheet = await _context.WeeklyLogs
                    .Include(w => w.DailyLogs)
                    .Where(w => w.Id == id)
                    .Select(w => new TimesheetResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        DateFrom = w.DateFrom,
                        DateTo = w.DateTo,
                        TotalHours = w.TotalHours,
                        TollCharges = w.TollCharges,
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
                    .FirstAsync();

                _logger.LogInformation("Successfully updated timesheet {TimesheetId} for user {UserId}", id, userId);

                return new TimesheetResult
                {
                    Success = true,
                    Message = "Timesheet updated successfully!",
                    Data = updatedTimesheet
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timesheet {TimesheetId} for user {UserId}", id, userId);
                
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
                _logger.LogInformation("Soft deleting timesheet {TimesheetId} for user {UserId}", id, userId);

                var existingTimesheet = await _context.WeeklyLogs
                    .Include(w => w.DailyLogs)
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId && !w.IsDeleted);

                if (existingTimesheet == null)
                {
                    _logger.LogWarning("Timesheet {TimesheetId} not found for user {UserId} during delete", id, userId);
                    
                    return new TimesheetResult
                    {
                        Success = false,
                        Message = "Timesheet not found",
                        Errors = ["The requested timesheet does not exist or you don't have permission to delete it"]
                    };
                }

                // Soft delete the weekly log
                existingTimesheet.IsDeleted = true;
                
                // Soft delete all associated daily logs
                foreach (var dailyLog in existingTimesheet.DailyLogs)
                {
                    dailyLog.IsDeleted = true;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully soft deleted timesheet {TimesheetId} for user {UserId}", id, userId);

                return new TimesheetResult
                {
                    Success = true,
                    Message = "Timesheet deleted successfully!",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting timesheet {TimesheetId} for user {UserId}", id, userId);
                
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