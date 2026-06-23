using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.DailyLog;
using EntryPointApp.Api.Models.Dtos.Manager;
using EntryPointApp.Api.Models.Entities;
using EntryPointApp.Api.Models.Enums;
using EntryPointApp.Api.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.SalesRep
{
    public class SalesRepService(
        ApplicationDbContext context,
        ILogger<SalesRepService> logger,
        IEmailService emailService,
        IConfiguration configuration) : ISalesRepService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<SalesRepService> _logger = logger;
        private readonly IEmailService _emailService = emailService;
        private readonly IConfiguration _configuration = configuration;

        public async Task<TeamTimesheetPagedResult> GetTeamTimesheetsAsync(int salesRepId, int page, int pageSize, string? statusFilter, string? search)
        {
            try
            {
                _logger.LogInformation("Retrieving team timesheets for sales rep {SalesRepId} - Page: {Page}, PageSize: {PageSize}, Filter: {Filter}",
                    salesRepId, page, pageSize, statusFilter ?? "All");

                var baseQuery = _context.Timesheet_WeeklyLogs
                    .Include(w => w.User)
                    .Where(w => w.User.SalesRepId == salesRepId && !w.IsDeleted);

                var statusCounts = await baseQuery
                    .GroupBy(w => w.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count);

                var query = baseQuery;

                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                {
                    if (Enum.TryParse<TimesheetStatus>(statusFilter, out var status))
                    {
                        query = query.Where(w => w.Status == status);
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.ToLower();
                    query = query.Where(w =>
                        w.User.Email.ToLower().Contains(q) ||
                        (w.User.FirstName + " " + w.User.LastName).ToLower().Contains(q));
                }

                var totalCount = await query.CountAsync();

                var timesheets = await query
                    .OrderBy(w => w.Status == TimesheetStatus.PendingSalesRep ? 0 : 1)
                    .ThenBy(w => w.CreatedAt)
                    .ThenBy(w => w.User.FirstName)
                    .ThenBy(w => w.User.LastName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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
                        SalesRepComment = w.SalesRepComment,
                        ManagerComment = w.ManagerComment,
                        SubmittedAt = w.CreatedAt,
                        UpdatedAt = w.UpdatedAt
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var pagedResult = new TeamTimesheetPagedResponse
                {
                    Data = timesheets,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1,
                    Summary = new TimesheetSummaryDto
                    {
                        TotalApproved        = statusCounts.GetValueOrDefault(TimesheetStatus.Approved),
                        TotalPending         = statusCounts.GetValueOrDefault(TimesheetStatus.PendingSalesRep),
                        TotalPendingManager  = statusCounts.GetValueOrDefault(TimesheetStatus.PendingManager),
                        TotalDenied          = statusCounts.GetValueOrDefault(TimesheetStatus.Denied)
                    }
                };

                _logger.LogInformation("Successfully retrieved {Count} timesheets for sales rep {SalesRepId}", timesheets.Count, salesRepId);

                return new TeamTimesheetPagedResult
                {
                    Success = true,
                    Message = "Team timesheets retrieved successfully!",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team timesheets for sales rep {SalesRepId}", salesRepId);
                return new TeamTimesheetPagedResult
                {
                    Success = false,
                    Message = "Failed to retrieve team timesheets",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<TeamTimesheetListResult> GetPendingTimesheetsAsync(int salesRepId)
        {
            try
            {
                _logger.LogInformation("Retrieving pending timesheets for sales rep {SalesRepId}", salesRepId);

                var timesheets = await _context.Timesheet_WeeklyLogs
                    .Include(w => w.User)
                    .Where(w => w.User.SalesRepId == salesRepId
                        && w.Status == TimesheetStatus.PendingSalesRep
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
                        SalesRepComment = w.SalesRepComment,
                        ManagerComment = w.ManagerComment,
                        SubmittedAt = w.CreatedAt,
                        UpdatedAt = w.UpdatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} pending timesheets for sales rep {SalesRepId}",
                    timesheets.Count, salesRepId);

                return new TeamTimesheetListResult
                {
                    Success = true,
                    Message = "Pending timesheets retrieved successfully!",
                    Data = timesheets
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending timesheets for sales rep {SalesRepId}", salesRepId);
                return new TeamTimesheetListResult
                {
                    Success = false,
                    Message = "Failed to retrieve pending timesheets",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<TeamTimesheetDetailResult> GetTimesheetDetailAsync(int weeklyLogId, int salesRepId)
        {
            try
            {
                _logger.LogInformation("Retrieving timesheet detail {WeeklyLogId} for sales rep {SalesRepId}",
                    weeklyLogId, salesRepId);

                var weeklyLog = await _context.Timesheet_WeeklyLogs
                    .Include(w => w.User)
                    .Include(w => w.DailyLogs.Where(d => !d.IsDeleted))
                        .ThenInclude(d => d.Attachments)
                    .Where(w => w.Id == weeklyLogId
                        && w.User.SalesRepId == salesRepId
                        && !w.IsDeleted)
                    .FirstOrDefaultAsync();

                if (weeklyLog == null)
                {
                    _logger.LogWarning("Timesheet {WeeklyLogId} not found for sales rep {SalesRepId}",
                        weeklyLogId, salesRepId);
                    return new TeamTimesheetDetailResult
                    {
                        Success = false,
                        Message = "Timesheet not found",
                        Errors = ["The requested timesheet does not exist or does not belong to your clients"]
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
                    SalesRepComment = weeklyLog.SalesRepComment,
                    ManagerComment = weeklyLog.ManagerComment,
                    SubmittedAt = weeklyLog.CreatedAt,
                    UpdatedAt = weeklyLog.UpdatedAt,
                    DailyLogs = weeklyLog.DailyLogs
                        .OrderBy(d => d.Date)
                        .Select(d => new TeamDailyLogResponse
                        {
                            Id = d.Id,
                            Date = d.Date,
                            TimeIn = d.TimeIn,
                            TimeOut = d.TimeOut,
                            Mileage = d.Mileage,
                            TollCharge = d.TollCharge,
                            ParkingFee = d.ParkingFee,
                            OtherCharges = d.OtherCharges,
                            Comment = d.Comment,
                            Receipts = d.Attachments
                                .OrderBy(a => a.UploadedAt)
                                .Select(a => new ReceiptResponse
                                {
                                    Id = a.Id,
                                    DailyLogId = a.DailyLogId,
                                    OriginalFileName = a.OriginalFileName,
                                    ContentType = a.ContentType,
                                    FileSizeBytes = a.FileSizeBytes,
                                    UploadedAt = a.UploadedAt
                                }).ToList()
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

        public async Task<TeamTimesheetResult> ApproveTimesheetAsync(int weeklyLogId, int salesRepId, ApproveTimesheetRequest request)
        {
            try
            {
                _logger.LogInformation("Sales rep {SalesRepId} approving timesheet {WeeklyLogId}",
                    salesRepId, weeklyLogId);

                var weeklyLog = await _context.Timesheet_WeeklyLogs
                    .Include(w => w.User)
                        .ThenInclude(u => u.Manager)
                    .FirstOrDefaultAsync(w => w.Id == weeklyLogId
                        && w.User.SalesRepId == salesRepId
                        && !w.IsDeleted);

                if (weeklyLog == null)
                {
                    _logger.LogWarning("Timesheet {WeeklyLogId} not found for sales rep {SalesRepId}",
                        weeklyLogId, salesRepId);
                    return new TeamTimesheetResult
                    {
                        Success = false,
                        Message = "Timesheet not found",
                        Errors = ["The requested timesheet does not exist or does not belong to your clients"]
                    };
                }

                if (weeklyLog.Status != TimesheetStatus.PendingSalesRep)
                {
                    return new TeamTimesheetResult
                    {
                        Success = false,
                        Message = "Cannot approve timesheet",
                        Errors = [$"Only timesheets pending Sales Rep review can be approved. Current status: {weeklyLog.Status}"]
                    };
                }

                var salesRep = await _context.Timesheet_Users.FirstOrDefaultAsync(u => u.Id == salesRepId);
                if (salesRep?.ManagerId == null)
                {
                    return new TeamTimesheetResult
                    {
                        Success = false,
                        Message = "Cannot approve timesheet",
                        Errors = ["You do not have a Manager assigned. Please contact your admin."]
                    };
                }

                var fromStatusApprove = weeklyLog.Status;
                weeklyLog.Status = TimesheetStatus.PendingManager;
                weeklyLog.SalesRepComment = request.Comment;
                weeklyLog.UpdatedAt = DateTime.UtcNow;

                _context.Timesheet_WeeklyLogStatusHistories.Add(new WeeklyLogStatusHistory
                {
                    WeeklyLogId = weeklyLogId,
                    ActorId = salesRepId,
                    FromStatus = fromStatusApprove,
                    ToStatus = TimesheetStatus.PendingManager,
                    Comment = request.Comment,
                    CreatedAt = weeklyLog.UpdatedAt
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Sales rep {SalesRepId} successfully approved timesheet {WeeklyLogId}", salesRepId, weeklyLogId);

                var manager = await _context.Timesheet_Users.FirstOrDefaultAsync(u => u.Id == salesRep.ManagerId);
                if (manager != null)
                {
                    var salesRepName = $"{salesRep.FirstName} {salesRep.LastName}".Trim();
                    var employeeName = $"{weeklyLog.User.FirstName} {weeklyLog.User.LastName}".Trim();
                    var weekPeriod = $"{weeklyLog.DateFrom:MMM d} - {weeklyLog.DateTo:MMM d, yyyy}";
                    var timesheetUrl = $"{_configuration["AppSettings:BaseUrl"]}/manager/timesheets/{weeklyLogId}";

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendManagerNotificationEmailAsync(
                                manager.Email,
                                $"{manager.FirstName} {manager.LastName}".Trim(),
                                salesRepName,
                                employeeName,
                                weekPeriod,
                                weeklyLog.TotalHours,
                                weeklyLog.TotalCharges,
                                timesheetUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send manager notification email for timesheet {WeeklyLogId}", weeklyLogId);
                        }
                    });
                }

                return new TeamTimesheetResult
                {
                    Success = true,
                    Message = "Timesheet approved — forwarded to manager for final approval.",
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
                        SalesRepComment = weeklyLog.SalesRepComment,
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

        public async Task<TeamTimesheetResult> DenyTimesheetAsync(int weeklyLogId, int salesRepId, DenyTimesheetRequest request)
        {
            try
            {
                _logger.LogInformation("Sales rep {SalesRepId} denying timesheet {WeeklyLogId}",
                    salesRepId, weeklyLogId);

                var weeklyLog = await _context.Timesheet_WeeklyLogs
                    .Include(w => w.User)
                    .FirstOrDefaultAsync(w => w.Id == weeklyLogId
                        && w.User.SalesRepId == salesRepId
                        && !w.IsDeleted);

                if (weeklyLog == null)
                {
                    _logger.LogWarning("Timesheet {WeeklyLogId} not found for sales rep {SalesRepId}",
                        weeklyLogId, salesRepId);
                    return new TeamTimesheetResult
                    {
                        Success = false,
                        Message = "Timesheet not found",
                        Errors = ["The requested timesheet does not exist or does not belong to your clients"]
                    };
                }

                if (weeklyLog.Status != TimesheetStatus.PendingSalesRep)
                {
                    return new TeamTimesheetResult
                    {
                        Success = false,
                        Message = "Cannot deny timesheet",
                        Errors = [$"Only timesheets pending Sales Rep review can be denied. Current status: {weeklyLog.Status}"]
                    };
                }

                var salesRep = await _context.Timesheet_Users.FirstOrDefaultAsync(u => u.Id == salesRepId);
                var salesRepName = salesRep != null
                    ? $"{salesRep.FirstName} {salesRep.LastName}".Trim()
                    : "Sales Rep";

                var fromStatusDeny = weeklyLog.Status;
                weeklyLog.Status = TimesheetStatus.Denied;
                weeklyLog.SalesRepComment = request.Reason;
                weeklyLog.UpdatedAt = DateTime.UtcNow;

                _context.Timesheet_WeeklyLogStatusHistories.Add(new WeeklyLogStatusHistory
                {
                    WeeklyLogId = weeklyLogId,
                    ActorId = salesRepId,
                    FromStatus = fromStatusDeny,
                    ToStatus = TimesheetStatus.Denied,
                    Comment = request.Reason,
                    CreatedAt = weeklyLog.UpdatedAt
                });

                await _context.SaveChangesAsync();

                _ = Task.Run(async () =>
                {
                    await _emailService.SendTimesheetDenialEmailAsync(
                        weeklyLog.User.Email,
                        $"{weeklyLog.User.FirstName} {weeklyLog.User.LastName}".Trim(),
                        salesRepName,
                        $"{weeklyLog.DateFrom:MMMM d, yyyy} - {weeklyLog.DateTo:MMMM d, yyyy}",
                        weeklyLog.TotalHours,
                        weeklyLog.TotalCharges,
                        request.Reason,
                        $"{_configuration["AppSettings:BaseUrl"]}/dashboard/week/{weeklyLog.Id}/edit"
                    );
                });

                _logger.LogInformation("Sales rep {SalesRepId} successfully denied timesheet {WeeklyLogId}", salesRepId, weeklyLogId);

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
                        SalesRepComment = weeklyLog.SalesRepComment,
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

        public async Task<TimesheetHistoryResult> GetTimesheetStatusHistoryAsync(int weeklyLogId, int salesRepId)
        {
            try
            {
                var exists = await _context.Timesheet_WeeklyLogs
                    .AnyAsync(w => w.Id == weeklyLogId
                        && w.User.SalesRepId == salesRepId
                        && !w.IsDeleted);

                if (!exists)
                {
                    return new TimesheetHistoryResult
                    {
                        Success = false,
                        Message = "Timesheet not found",
                        Errors = ["The requested timesheet does not exist or does not belong to your clients"]
                    };
                }

                var history = await _context.Timesheet_WeeklyLogStatusHistories
                    .Include(h => h.Actor)
                    .Where(h => h.WeeklyLogId == weeklyLogId)
                    .OrderBy(h => h.CreatedAt)
                    .Select(h => new TimesheetStatusHistoryResponse
                    {
                        Id = h.Id,
                        ActorFullName = $"{h.Actor.FirstName} {h.Actor.LastName}".Trim(),
                        ActorRole = h.Actor.Role.ToString(),
                        FromStatus = h.FromStatus.ToString(),
                        ToStatus = h.ToStatus.ToString(),
                        Comment = h.Comment,
                        CreatedAt = h.CreatedAt
                    })
                    .ToListAsync();

                return new TimesheetHistoryResult
                {
                    Success = true,
                    Message = "Timesheet status history retrieved successfully.",
                    Data = history
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status history for timesheet {WeeklyLogId}", weeklyLogId);
                return new TimesheetHistoryResult
                {
                    Success = false,
                    Message = "Failed to retrieve timesheet status history",
                    Errors = [ex.Message]
                };
            }
        }
    }
}
