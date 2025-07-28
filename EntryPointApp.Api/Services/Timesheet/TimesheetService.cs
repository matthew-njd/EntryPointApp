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

                var query = _context.WeeklyLogs.Where(w => w.UserId == userId);

                if (request.StartDate.HasValue)
                {
                    query = query.Where(w => w.Date >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(w => w.Date <= request.EndDate.Value);
                }

                var totalCount = await query.CountAsync();

                var timesheets = await query
                    .OrderByDescending(w => w.Date)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(w => new TimesheetResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        Date = w.Date,
                        Hours = w.Hours,
                        Mileage = w.Mileage,
                        TollCharge = w.TollCharge,
                        ParkingFee = w.ParkingFee,
                        OtherCharges = w.OtherCharges,
                        Comment = w.Comment
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

                return new TimesheetListResult
                {
                    Success = false,
                    Message = "Failed to retrieve timesheets",
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
                    .Where(w => w.Id == id && w.UserId == userId)
                    .Select(w => new TimesheetResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        Date = w.Date,
                        Hours = w.Hours,
                        Mileage = w.Mileage,
                        TollCharge = w.TollCharge,
                        ParkingFee = w.ParkingFee,
                        OtherCharges = w.OtherCharges,
                        Comment = w.Comment
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
                _logger.LogInformation("Creating timesheet for user {UserId} on date {Date}", userId, request.Date);

                var weeklyLog = new WeeklyLog
                {
                    UserId = userId,
                    Date = request.Date,
                    Hours = request.Hours,
                    Mileage = request.Mileage,
                    TollCharge = request.TollCharge,
                    ParkingFee = request.ParkingFee,
                    OtherCharges = request.OtherCharges,
                    Comment = request.Comment
                };

                _context.WeeklyLogs.Add(weeklyLog);
                await _context.SaveChangesAsync();

                var timesheetResponse = new TimesheetResponse
                {
                    Id = weeklyLog.Id,
                    UserId = weeklyLog.UserId,
                    Date = weeklyLog.Date,
                    Hours = weeklyLog.Hours,
                    Mileage = weeklyLog.Mileage,
                    TollCharge = weeklyLog.TollCharge,
                    ParkingFee = weeklyLog.ParkingFee,
                    OtherCharges = weeklyLog.OtherCharges,
                    Comment = weeklyLog.Comment
                };

                _logger.LogInformation("Successfully created timesheet {TimesheetId} for user {UserId}", 
                    weeklyLog.Id, userId);

                return new TimesheetResult
                {
                    Success = true,
                    Message = "Timesheet created successfully!",
                    Data = timesheetResponse
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
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

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

                existingTimesheet.Date = request.Date;
                existingTimesheet.Hours = request.Hours;
                existingTimesheet.Mileage = request.Mileage;
                existingTimesheet.TollCharge = request.TollCharge;
                existingTimesheet.ParkingFee = request.ParkingFee;
                existingTimesheet.OtherCharges = request.OtherCharges;
                existingTimesheet.Comment = request.Comment;

                await _context.SaveChangesAsync();

                var timesheetResponse = new TimesheetResponse
                {
                    Id = existingTimesheet.Id,
                    UserId = existingTimesheet.UserId,
                    Date = existingTimesheet.Date,
                    Hours = existingTimesheet.Hours,
                    Mileage = existingTimesheet.Mileage,
                    TollCharge = existingTimesheet.TollCharge,
                    ParkingFee = existingTimesheet.ParkingFee,
                    OtherCharges = existingTimesheet.OtherCharges,
                    Comment = existingTimesheet.Comment
                };

                _logger.LogInformation("Successfully updated timesheet {TimesheetId} for user {UserId}", id, userId);

                return new TimesheetResult
                {
                    Success = true,
                    Message = "Timesheet updated successfully!",
                    Data = timesheetResponse
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

                existingTimesheet.IsDeleted = true;

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