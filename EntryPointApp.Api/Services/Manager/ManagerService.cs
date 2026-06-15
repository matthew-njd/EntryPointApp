using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.DailyLog;
using EntryPointApp.Api.Models.Dtos.Manager;
using EntryPointApp.Api.Models.Enums;
using EntryPointApp.Api.Services.Email;
using EntryPointApp.Api.Services.Excel;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.Manager
{
    public class ManagerService(
        ApplicationDbContext context,
        ILogger<ManagerService> logger,
        IExcelService excelService,
        IEmailService emailService,
        IConfiguration configuration) : IManagerService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<ManagerService> _logger = logger;
        private readonly IExcelService _excelService = excelService;
        private readonly IEmailService _emailService = emailService;
        private readonly IConfiguration _configuration = configuration;

        public async Task<TeamTimesheetPagedResult> GetTeamTimesheetsAsync(int managerId, int page, int pageSize, string? statusFilter, string? search)
        {
            try
            {
                _logger.LogInformation("Retrieving team timesheets for manager {ManagerId} - Page: {Page}, PageSize: {PageSize}, Filter: {Filter}",
                    managerId, page, pageSize, statusFilter ?? "All");

                var baseQuery = _context.Timesheet_WeeklyLogs
                    .Include(w => w.User)
                    .Where(w => w.User.ManagerId == managerId && !w.IsDeleted);

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
                    .OrderBy(w => w.Status == TimesheetStatus.Pending ? 0 : 1)
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
                        TotalApproved = statusCounts.GetValueOrDefault(TimesheetStatus.Approved),
                        TotalPending  = statusCounts.GetValueOrDefault(TimesheetStatus.Pending),
                        TotalDenied   = statusCounts.GetValueOrDefault(TimesheetStatus.Denied)
                    }
                };

                _logger.LogInformation("Successfully retrieved {Count} timesheets (page {Page} of {TotalPages}) for manager {ManagerId}",
                    timesheets.Count, page, totalPages, managerId);

                return new TeamTimesheetPagedResult
                {
                    Success = true,
                    Message = "Team timesheets retrieved successfully!",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team timesheets for manager {ManagerId}", managerId);
                return new TeamTimesheetPagedResult
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

                var timesheets = await _context.Timesheet_WeeklyLogs
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

                var weeklyLog = await _context.Timesheet_WeeklyLogs
                    .Include(w => w.User)
                    .Include(w => w.DailyLogs.Where(d => !d.IsDeleted))
                        .ThenInclude(d => d.Attachments)
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

        public async Task<TeamTimesheetResult> ApproveTimesheetAsync(int weeklyLogId, int managerId, ApproveTimesheetRequest request)
        {
            try
            {
                _logger.LogInformation("Manager {ManagerId} approving timesheet {WeeklyLogId}",
                    managerId, weeklyLogId);

                var weeklyLog = await _context.Timesheet_WeeklyLogs
                    .Include(w => w.User)
                        .ThenInclude(u => u.Manager)
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

                var employeeName = $"{weeklyLog.User.FirstName} {weeklyLog.User.LastName}".Trim();
                var managerName = $"{weeklyLog.User.Manager!.FirstName} {weeklyLog.User.Manager.LastName}".Trim();
                var weekPeriod = $"{weeklyLog.DateFrom:MM/dd/yyyy} - {weeklyLog.DateTo:MM/dd/yyyy}";
                var filename = $"Timesheet_{employeeName.Replace(" ", "_")}_{weeklyLog.DateFrom:yyyyMMdd}.xlsx";
                var userEmail = weeklyLog.User.Email;
                var managerEmail = weeklyLog.User.Manager.Email;
                var totalHours = weeklyLog.TotalHours;
                var totalCharges = weeklyLog.TotalCharges;

                var weeklyLogDateFrom = weeklyLog.DateFrom.ToDateTime(TimeOnly.MinValue);
                var userRate = await _context.Timesheet_UserRates
                    .Where(r => r.UserId == weeklyLog.UserId && r.EffectiveDate <= weeklyLogDateFrom)
                    .OrderByDescending(r => r.EffectiveDate)
                    .FirstOrDefaultAsync();
                var hourlyRate = userRate?.HourlyRate ?? 0m;
                var mileageRate = userRate?.MileageRate ?? 0m;

                var totalMileage = await _context.Timesheet_DailyLogs
                    .Where(d => d.WeeklyLogId == weeklyLogId && !d.IsDeleted)
                    .SumAsync(d => d.Mileage);

                var totalPay = hourlyRate * totalHours;
                var mileagePay = mileageRate * totalMileage;

                var excelBytes = await _excelService.GenerateTimesheetExcelAsync(weeklyLogId, hourlyRate, mileageRate);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendTimesheetApprovalEmailAsync(
                            userEmail,
                            employeeName,
                            managerEmail,
                            managerName,
                            weekPeriod,
                            totalHours,
                            totalCharges,
                            hourlyRate,
                            totalPay,
                            mileageRate,
                            mileagePay,
                            excelBytes,
                            filename);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send approval email for timesheet {WeeklyLogId}", weeklyLogId);
                    }
                });

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

                var weeklyLog = await _context.Timesheet_WeeklyLogs
                    .Include(w => w.User)
                        .ThenInclude(u => u.Manager)
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

                _ = Task.Run(async () =>
                {
                    await _emailService.SendTimesheetDenialEmailAsync(
                        weeklyLog.User.Email!,
                        $"{weeklyLog.User.FirstName} {weeklyLog.User.LastName}".Trim(),
                        $"{weeklyLog.User.Manager!.FirstName} {weeklyLog.User.Manager.LastName}".Trim(),
                        $"{weeklyLog.DateFrom:MMMM d, yyyy} - {weeklyLog.DateTo:MMMM d, yyyy}",
                        weeklyLog.TotalHours,
                        weeklyLog.TotalCharges,
                        request.Reason,
                        $"{_configuration["AppSettings:BaseUrl"]}/dashboard/week/{weeklyLog.Id}/edit"
                    );
                });

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