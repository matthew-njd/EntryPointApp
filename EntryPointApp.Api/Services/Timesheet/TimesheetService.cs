using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Common;
using EntryPointApp.Api.Models.Dtos.Timesheets;
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

        public Task<TimesheetResponse?> GetTimesheetByIdAsync(int id, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<TimesheetResponse> CreateTimesheetAsync(TimesheetRequest request, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<TimesheetResponse?> UpdateTimesheetAsync(int id, TimesheetRequest request, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteTimesheetAsync(int id, int userId)
        {
            throw new NotImplementedException();
        }
    }
}