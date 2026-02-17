using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Manager;
using EntryPointApp.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.Manager
{
public class ManagerService(
        ApplicationDbContext context,
        ILogger<ManagerService> logger) : IManagerService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<ManagerService> _logger = logger;

        public async Task<TeamTimesheetListResult> GetTeamTimesheetsAsync(int managerId, string? statusFilter)
        {
            try
            {
                _logger.LogInformation("Retrieving team timesheets for manager {ManagerId} with filter {Filter}",
                    managerId, statusFilter ?? "All");

                var query = _context.WeeklyLogs
                    .Include(w => w.User)
                    .Where(w => w.User.ManagerId == managerId && !w.IsDeleted);

                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                {
                    if (Enum.TryParse<TimesheetStatus>(statusFilter, out var status))
                    {
                        query = query.Where(w => w.Status == status);
                    }
                }

                var timesheets = await query
                    .OrderByDescending(w => w.UpdatedAt)
                    .Select(w => new TeamTimesheetResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        UserFullName = $"{w.User.FirstName} {w.User.LastName}".Trim(),
                        UserEmail = w.User.Email,
                        DateFrom = w.DateFrom,
                        DateTo = w.DateTo,
                        TotalHours = w.TotalHours,
                        TotalCharges = w.TotalCharges,
                        Status = w.Status.ToString(),
                        ManagerComment = w.ManagerComment,
                        SubmittedAt = w.CreatedAt,
                        UpdatedAt = w.UpdatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} timesheets for manager {ManagerId}",
                    timesheets.Count, managerId);

                return new TeamTimesheetListResult
                {
                    Success = true,
                    Message = "Team timesheets retrieved successfully!",
                    Data = timesheets
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team timesheets for manager {ManagerId}", managerId);
                return new TeamTimesheetListResult
                {
                    Success = false,
                    Message = "Failed to retrieve team timesheets",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<TeamTimesheetListResult> GetPendingTimesheetsAsync(int managerId)
        {
            try
            {
                _logger.LogInformation("Retrieving pending timesheets for manager {ManagerId}", managerId);

                var timesheets = await _context.WeeklyLogs
                    .Include(w => w.User)
                    .Where(w => w.User.ManagerId == managerId
                        && w.Status == TimesheetStatus.Pending
                        && !w.IsDeleted)
                    .OrderBy(w => w.UpdatedAt)
                    .Select(w => new TeamTimesheetResponse
                    {
                        Id = w.Id,
                        UserId = w.UserId,
                        UserFullName = $"{w.User.FirstName} {w.User.LastName}".Trim(),
                        UserEmail = w.User.Email,
                        DateFrom = w.DateFrom,
                        DateTo = w.DateTo,
                        TotalHours = w.TotalHours,
                        TotalCharges = w.TotalCharges,
                        Status = w.Status.ToString(),
                        ManagerComment = w.ManagerComment,
                        SubmittedAt = w.CreatedAt,
                        UpdatedAt = w.UpdatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} pending timesheets for manager {ManagerId}",
                    timesheets.Count, managerId);

                return new TeamTimesheetListResult
                {
                    Success = true,
                    Message = "Pending timesheets retrieved successfully!",
                    Data = timesheets
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending timesheets for manager {ManagerId}", managerId);
                return new TeamTimesheetListResult
                {
                    Success = false,
                    Message = "Failed to retrieve pending timesheets",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<TeamTimesheetDetailResult> GetTimesheetDetailAsync(int weeklyLogId, int managerId)
        {
            try
            {
                _logger.LogInformation("Retrieving timesheet detail {WeeklyLogId} for manager {ManagerId}",
                    weeklyLogId, managerId);

                var weeklyLog = await _context.WeeklyLogs
                    .Include(w => w.User)
                    .Include(w => w.DailyLogs.Where(d => !d.IsDeleted))
                    .Where(w => w.Id == weeklyLogId
                        && w.User.ManagerId == managerId
                        && !w.IsDeleted)
                    .FirstOrDefaultAsync();

                if (weeklyLog == null)
                {
                    _logger.LogWarning("Timesheet {WeeklyLogId} not found for manager {ManagerId}",
                        weeklyLogId, managerId);
                    return new TeamTimesheetDetailResult
                    {
                        Success = false,
                        Message = "Timesheet not found",
                        Errors = ["The requested timesheet does not exist or does not belong to your team"]
                    };
                }

                var detail = new TeamTimesheetDetailResponse
                {
                    Id = weeklyLog.Id,
                    UserId = weeklyLog.UserId,
                    UserFullName = $"{weeklyLog.User.FirstName} {weeklyLog.User.LastName}".Trim(),
                    UserEmail = weeklyLog.User.Email,
                    DateFrom = weeklyLog.DateFrom,
                    DateTo = weeklyLog.DateTo,
                    TotalHours = weeklyLog.TotalHours,
                    TotalCharges = weeklyLog.TotalCharges,
                    Status = weeklyLog.Status.ToString(),
                    ManagerComment = weeklyLog.ManagerComment,
                    SubmittedAt = weeklyLog.CreatedAt,
                    UpdatedAt = weeklyLog.UpdatedAt,
                    DailyLogs = weeklyLog.DailyLogs
                        .OrderBy(d => d.Date)
                        .Select(d => new TeamDailyLogResponse
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
                        .ToList()
                };

                _logger.LogInformation("Successfully retrieved timesheet detail {WeeklyLogId}", weeklyLogId);

                return new TeamTimesheetDetailResult
                {
                    Success = true,
                    Message = "Timesheet detail retrieved successfully!",
                    Data = detail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving timesheet detail {WeeklyLogId}", weeklyLogId);
                return new TeamTimesheetDetailResult
                {
                    Success = false,
                    Message = "Failed to retrieve timesheet detail",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<TeamTimesheetResult> ApproveTimesheetAsync(int weeklyLogId, int managerId, ApproveTimesheetRequest request)
        {
            try
            {
                _logger.LogInformation("Manager {ManagerId} approving timesheet {WeeklyLogId}",
                    managerId, weeklyLogId);

                var weeklyLog = await _context.WeeklyLogs
                    .Include(w => w.User)
                    .FirstOrDefaultAsync(w => w.Id == weeklyLogId
                        && w.User.ManagerId == managerId
                        && !w.IsDeleted);

                if (weeklyLog == null)
                {
                    _logger.LogWarning("Timesheet {WeeklyLogId} not found for manager {ManagerId}",
                        weeklyLogId, managerId);
                    return new TeamTimesheetResult
                    {
                        Success = false,
                        Message = "Timesheet not found",
                        Errors = ["The requested timesheet does not exist or does not belong to your team"]
                    };
                }

                if (weeklyLog.Status != TimesheetStatus.Pending)
                {
                    return new TeamTimesheetResult
                    {
                        Success = false,
                        Message = "Cannot approve timesheet",
                        Errors = [$"Only Pending timesheets can be approved. Current status: {weeklyLog.Status}"]
                    };
                }

                weeklyLog.Status = TimesheetStatus.Approved;
                weeklyLog.ManagerComment = request.Comment;
                weeklyLog.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Manager {ManagerId} successfully approved timesheet {WeeklyLogId}",
                    managerId, weeklyLogId);

                return new TeamTimesheetResult
                {
                    Success = true,
                    Message = "Timesheet approved successfully!",
                    Data = new TeamTimesheetResponse
                    {
                        Id = weeklyLog.Id,
                        UserId = weeklyLog.UserId,
                        UserFullName = $"{weeklyLog.User.FirstName} {weeklyLog.User.LastName}".Trim(),
                        UserEmail = weeklyLog.User.Email,
                        DateFrom = weeklyLog.DateFrom,
                        DateTo = weeklyLog.DateTo,
                        TotalHours = weeklyLog.TotalHours,
                        TotalCharges = weeklyLog.TotalCharges,
                        Status = weeklyLog.Status.ToString(),
                        ManagerComment = weeklyLog.ManagerComment,
                        SubmittedAt = weeklyLog.CreatedAt,
                        UpdatedAt = weeklyLog.UpdatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving timesheet {WeeklyLogId}", weeklyLogId);
                return new TeamTimesheetResult
                {
                    Success = false,
                    Message = "Failed to approve timesheet",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<TeamTimesheetResult> DenyTimesheetAsync(int weeklyLogId, int managerId, DenyTimesheetRequest request)
        {
            try
            {
                _logger.LogInformation("Manager {ManagerId} denying timesheet {WeeklyLogId}",
                    managerId, weeklyLogId);

                var weeklyLog = await _context.WeeklyLogs
                    .Include(w => w.User)
                    .FirstOrDefaultAsync(w => w.Id == weeklyLogId
                        && w.User.ManagerId == managerId
                        && !w.IsDeleted);

                if (weeklyLog == null)
                {
                    _logger.LogWarning("Timesheet {WeeklyLogId} not found for manager {ManagerId}",
                        weeklyLogId, managerId);
                    return new TeamTimesheetResult
                    {
                        Success = false,
                        Message = "Timesheet not found",
                        Errors = ["The requested timesheet does not exist or does not belong to your team"]
                    };
                }

                if (weeklyLog.Status != TimesheetStatus.Pending)
                {
                    return new TeamTimesheetResult
                    {
                        Success = false,
                        Message = "Cannot deny timesheet",
                        Errors = [$"Only Pending timesheets can be denied. Current status: {weeklyLog.Status}"]
                    };
                }

                weeklyLog.Status = TimesheetStatus.Denied;
                weeklyLog.ManagerComment = request.Reason;
                weeklyLog.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Manager {ManagerId} successfully denied timesheet {WeeklyLogId}",
                    managerId, weeklyLogId);

                return new TeamTimesheetResult
                {
                    Success = true,
                    Message = "Timesheet denied successfully!",
                    Data = new TeamTimesheetResponse
                    {
                        Id = weeklyLog.Id,
                        UserId = weeklyLog.UserId,
                        UserFullName = $"{weeklyLog.User.FirstName} {weeklyLog.User.LastName}".Trim(),
                        UserEmail = weeklyLog.User.Email,
                        DateFrom = weeklyLog.DateFrom,
                        DateTo = weeklyLog.DateTo,
                        TotalHours = weeklyLog.TotalHours,
                        TotalCharges = weeklyLog.TotalCharges,
                        Status = weeklyLog.Status.ToString(),
                        ManagerComment = weeklyLog.ManagerComment,
                        SubmittedAt = weeklyLog.CreatedAt,
                        UpdatedAt = weeklyLog.UpdatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error denying timesheet {WeeklyLogId}", weeklyLogId);
                return new TeamTimesheetResult
                {
                    Success = false,
                    Message = "Failed to deny timesheet",
                    Errors = [ex.Message]
                };
            }
        }
    }
}