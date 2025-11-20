using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.WeeklyLog;
using EntryPointApp.Api.Services.WeeklyLog;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.Weeklylog
{
    public class WeeklylogService(ApplicationDbContext context, ILogger<WeeklylogService> logger) : IWeeklyLogService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<WeeklylogService> _logger = logger;

        public async Task<WeeklyLogListResult> GetWeeklyLogsAsync(int userId, PagedRequest request)
        {
            try
            {
                _logger.LogInformation("Retrieving timesheets for user {UserId} - Page: {Page}, PageSize: {PageSize}",
                        userId, request.Page, request.PageSize);

                var query = _context.WeeklyLogs.Where(w => w.UserId == userId && !w.IsDeleted);

                if (request.StartDate.HasValue)
                {
                    query = query.Where(w => w.DateFrom >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(w => w.DateTo <= request.EndDate.Value);
                }

                var totalCount = await query.CountAsync();

                var weeklylogs = await query.OrderByDescending(w => w.DateFrom).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(w => new WeeklyLogResponse
                {
                        Id = w.Id,
                        UserId = w.UserId,
                        DateFrom = w.DateFrom,
                        DateTo = w.DateTo,
                        TotalHours = w.TotalHours,
                        TotalCharges = w.TotalCharges,
                        Status = w.Status,
                }).ToListAsync();

                 var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var pagedResult = new PagedResult<WeeklyLogResponse>
                {
                    Data = weeklylogs,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1
                };

                _logger.LogInformation("Successfully retrieved {Count} timesheets for user {UserId}", 
                    weeklylogs.Count, userId);

                return new WeeklyLogListResult
                {
                    Success = true,
                    Message = "WeeklyLogs retrieved successfully!",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving timesheets for user {UserId}", userId);

                return new WeeklyLogListResult
                {
                    Success = false,
                    Message = "Failed to retrieve timesheets",
                    Errors = [ex.Message]
                };
            }
           
        }

        public Task<WeeklyLogResult> GetWeeklyLogByIdAsync(int id, int userId)
        {
            throw new NotImplementedException();
        }
    }
}