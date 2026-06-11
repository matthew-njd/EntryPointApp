using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.DailyLog;
using EntryPointApp.Api.Models.Entities;
using EntryPointApp.Api.Models.Enums;
using EntryPointApp.Api.Services.FileStorage;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.Receipt
{
    public class ReceiptService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        ILogger<ReceiptService> logger) : IReceiptService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly ILogger<ReceiptService> _logger = logger;

        public async Task<ReceiptResult> UploadReceiptAsync(int dailyLogId, int weeklyLogId, IFormFile file, int requestingUserId)
        {
            try
            {
                var (access, dailyLog) = await CheckAccessAsync(dailyLogId, weeklyLogId, requestingUserId);

                if (access == AccessResult.NotFound)
                    return new ReceiptResult
                    {
                        Success = false,
                        Message = "Daily log not found",
                        Errors = ["The requested daily log does not exist or has been deleted"]
                    };

                if (access == AccessResult.Forbidden)
                    return new ReceiptResult
                    {
                        Success = false,
                        Message = "Access denied",
                        Errors = ["You do not have permission to upload receipts for this daily log"]
                    };

                string savedFileName;
                try
                {
                    savedFileName = await _fileStorageService.SaveFileAsync(file, dailyLog!.UserId, weeklyLogId);
                }
                catch (ArgumentException ex)
                {
                    return new ReceiptResult
                    {
                        Success = false,
                        Message = "File validation failed",
                        Errors = [ex.Message]
                    };
                }

                var attachment = new DailyLogAttachment
                {
                    DailyLogId = dailyLogId,
                    FileName = savedFileName,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length,
                    UploadedAt = DateTime.UtcNow
                };

                _context.Timesheet_DailyLogAttachments.Add(attachment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} uploaded receipt {AttachmentId} for daily log {DailyLogId}",
                    requestingUserId, attachment.Id, dailyLogId);

                return new ReceiptResult
                {
                    Success = true,
                    Message = "Receipt uploaded successfully!",
                    Data = MapToResponse(attachment)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading receipt for daily log {DailyLogId}", dailyLogId);
                return new ReceiptResult
                {
                    Success = false,
                    Message = "Failed to upload receipt",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<ReceiptListResult> GetReceiptsAsync(int dailyLogId, int weeklyLogId, int requestingUserId)
        {
            try
            {
                var (access, _) = await CheckAccessAsync(dailyLogId, weeklyLogId, requestingUserId);

                if (access == AccessResult.NotFound)
                    return new ReceiptListResult
                    {
                        Success = false,
                        Message = "Daily log not found",
                        Errors = ["The requested daily log does not exist or has been deleted"]
                    };

                if (access == AccessResult.Forbidden)
                    return new ReceiptListResult
                    {
                        Success = false,
                        Message = "Access denied",
                        Errors = ["You do not have permission to view receipts for this daily log"]
                    };

                var receipts = await _context.Timesheet_DailyLogAttachments
                    .Where(a => a.DailyLogId == dailyLogId)
                    .OrderBy(a => a.UploadedAt)
                    .Select(a => new ReceiptResponse
                    {
                        Id = a.Id,
                        DailyLogId = a.DailyLogId,
                        OriginalFileName = a.OriginalFileName,
                        ContentType = a.ContentType,
                        FileSizeBytes = a.FileSizeBytes,
                        UploadedAt = a.UploadedAt
                    })
                    .ToListAsync();

                return new ReceiptListResult
                {
                    Success = true,
                    Message = "Receipts retrieved successfully!",
                    Data = receipts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipts for daily log {DailyLogId}", dailyLogId);
                return new ReceiptListResult
                {
                    Success = false,
                    Message = "Failed to retrieve receipts",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<ReceiptFileResult> DownloadReceiptAsync(int attachmentId, int dailyLogId, int weeklyLogId, int requestingUserId)
        {
            try
            {
                var (access, _) = await CheckAccessAsync(dailyLogId, weeklyLogId, requestingUserId);

                if (access == AccessResult.NotFound)
                    return new ReceiptFileResult
                    {
                        Success = false,
                        Message = "Daily log not found",
                        Errors = ["The requested daily log does not exist or has been deleted"]
                    };

                if (access == AccessResult.Forbidden)
                    return new ReceiptFileResult
                    {
                        Success = false,
                        Message = "Access denied",
                        Errors = ["You do not have permission to download receipts for this daily log"]
                    };

                var attachment = await _context.Timesheet_DailyLogAttachments
                    .FirstOrDefaultAsync(a => a.Id == attachmentId && a.DailyLogId == dailyLogId);

                if (attachment == null)
                    return new ReceiptFileResult
                    {
                        Success = false,
                        Message = "Receipt not found",
                        Errors = ["The requested receipt does not exist"]
                    };

                if (!_fileStorageService.FileExists(attachment.FileName))
                {
                    _logger.LogError("Receipt file missing on disk for attachment {AttachmentId}: {FileName}",
                        attachmentId, attachment.FileName);
                    return new ReceiptFileResult
                    {
                        Success = false,
                        Message = "Receipt file not found",
                        Errors = ["The receipt file could not be located on the server"]
                    };
                }

                return new ReceiptFileResult
                {
                    Success = true,
                    Message = "Receipt retrieved successfully!",
                    FileStream = _fileStorageService.OpenFileStream(attachment.FileName),
                    ContentType = attachment.ContentType,
                    FileName = attachment.OriginalFileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading receipt {AttachmentId}", attachmentId);
                return new ReceiptFileResult
                {
                    Success = false,
                    Message = "Failed to download receipt",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<BaseDailyLogResult> DeleteReceiptAsync(int attachmentId, int dailyLogId, int weeklyLogId, int requestingUserId)
        {
            try
            {
                var (access, _) = await CheckAccessAsync(dailyLogId, weeklyLogId, requestingUserId, requireOwnerOrAdmin: true);

                if (access == AccessResult.NotFound)
                    return new BaseDailyLogResult
                    {
                        Success = false,
                        Message = "Daily log not found",
                        Errors = ["The requested daily log does not exist or has been deleted"]
                    };

                if (access == AccessResult.Forbidden)
                    return new BaseDailyLogResult
                    {
                        Success = false,
                        Message = "Access denied",
                        Errors = ["Only the owner or an admin can delete receipts"]
                    };

                var attachment = await _context.Timesheet_DailyLogAttachments
                    .FirstOrDefaultAsync(a => a.Id == attachmentId && a.DailyLogId == dailyLogId);

                if (attachment == null)
                    return new BaseDailyLogResult
                    {
                        Success = false,
                        Message = "Receipt not found",
                        Errors = ["The requested receipt does not exist"]
                    };

                _fileStorageService.DeleteFile(attachment.FileName);

                _context.Timesheet_DailyLogAttachments.Remove(attachment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted receipt {AttachmentId} for daily log {DailyLogId}",
                    requestingUserId, attachmentId, dailyLogId);

                return new BaseDailyLogResult
                {
                    Success = true,
                    Message = "Receipt deleted successfully!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting receipt {AttachmentId}", attachmentId);
                return new BaseDailyLogResult
                {
                    Success = false,
                    Message = "Failed to delete receipt",
                    Errors = [ex.Message]
                };
            }
        }

        private enum AccessResult { Allowed, Forbidden, NotFound }

        private async Task<(AccessResult result, Models.Entities.DailyLog? dailyLog)> CheckAccessAsync(
            int dailyLogId,
            int weeklyLogId,
            int requestingUserId,
            bool requireOwnerOrAdmin = false)
        {
            var dailyLog = await _context.Timesheet_DailyLogs
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == dailyLogId && d.WeeklyLogId == weeklyLogId && !d.IsDeleted);

            if (dailyLog == null)
                return (AccessResult.NotFound, null);

            var requestingUser = await _context.Timesheet_Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == requestingUserId);

            if (requestingUser == null)
                return (AccessResult.Forbidden, null);

            bool isOwner = dailyLog.UserId == requestingUserId;
            bool isAdmin = requestingUser.Role == UserRole.Admin;
            bool isManager = requestingUser.Role == UserRole.Manager
                          && dailyLog.User.ManagerId.HasValue
                          && dailyLog.User.ManagerId.Value == requestingUserId;

            if (requireOwnerOrAdmin)
            {
                return isOwner || isAdmin
                    ? (AccessResult.Allowed, dailyLog)
                    : (AccessResult.Forbidden, null);
            }

            return isOwner || isAdmin || isManager
                ? (AccessResult.Allowed, dailyLog)
                : (AccessResult.Forbidden, null);
        }

        private static ReceiptResponse MapToResponse(DailyLogAttachment attachment) => new()
        {
            Id = attachment.Id,
            DailyLogId = attachment.DailyLogId,
            OriginalFileName = attachment.OriginalFileName,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes,
            UploadedAt = attachment.UploadedAt
        };
    }
}
