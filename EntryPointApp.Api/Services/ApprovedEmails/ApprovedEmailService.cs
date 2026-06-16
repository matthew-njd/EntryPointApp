using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.ApprovedEmails;
using EntryPointApp.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.ApprovedEmails
{
    public class ApprovedEmailService(ApplicationDbContext context) : IApprovedEmailService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<ApprovedEmailListResult> GetAllAsync()
        {
            try
            {
                var emails = await _context.Timesheet_ApprovedEmails
                    .OrderBy(e => e.Email)
                    .Select(e => new ApprovedEmailResponse
                    {
                        Id = e.Id,
                        Email = e.Email,
                        AddedByAdminId = e.AddedByAdminId,
                        AddedByAdminName = e.AddedByAdmin != null
                            ? e.AddedByAdmin.FirstName + " " + e.AddedByAdmin.LastName
                            : null,
                        CreatedAt = e.CreatedAt
                    })
                    .ToListAsync();

                return new ApprovedEmailListResult
                {
                    Success = true,
                    Message = "Approved emails retrieved successfully.",
                    Data = emails
                };
            }
            catch (Exception)
            {
                return new ApprovedEmailListResult
                {
                    Success = false,
                    Message = "An error occurred while retrieving approved emails.",
                    Errors = ["Internal server error"]
                };
            }
        }

        public async Task<ApprovedEmailResult> AddAsync(AddApprovedEmailRequest request, int adminId)
        {
            try
            {
                var normalizedEmail = request.Email.ToLowerInvariant();

                var exists = await _context.Timesheet_ApprovedEmails
                    .AnyAsync(e => e.Email == normalizedEmail);

                if (exists)
                {
                    return new ApprovedEmailResult
                    {
                        Success = false,
                        Message = "This email is already on the approved list.",
                        Errors = ["Email already exists"]
                    };
                }

                var approvedEmail = new ApprovedEmail
                {
                    Email = normalizedEmail,
                    AddedByAdminId = adminId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Timesheet_ApprovedEmails.Add(approvedEmail);
                await _context.SaveChangesAsync();

                var admin = await _context.Timesheet_Users.FindAsync(adminId);

                return new ApprovedEmailResult
                {
                    Success = true,
                    Message = "Email approved successfully.",
                    Data = new ApprovedEmailResponse
                    {
                        Id = approvedEmail.Id,
                        Email = approvedEmail.Email,
                        AddedByAdminId = approvedEmail.AddedByAdminId,
                        AddedByAdminName = admin != null
                            ? admin.FirstName + " " + admin.LastName
                            : null,
                        CreatedAt = approvedEmail.CreatedAt
                    }
                };
            }
            catch (Exception)
            {
                return new ApprovedEmailResult
                {
                    Success = false,
                    Message = "An error occurred while adding the approved email.",
                    Errors = ["Internal server error"]
                };
            }
        }

        public async Task<BaseApprovedEmailResult> RemoveAsync(int id)
        {
            try
            {
                var approvedEmail = await _context.Timesheet_ApprovedEmails.FindAsync(id);

                if (approvedEmail == null)
                {
                    return new BaseApprovedEmailResult
                    {
                        Success = false,
                        Message = "Approved email not found.",
                        Errors = ["Not found"]
                    };
                }

                _context.Timesheet_ApprovedEmails.Remove(approvedEmail);
                await _context.SaveChangesAsync();

                return new BaseApprovedEmailResult
                {
                    Success = true,
                    Message = "Email removed from approved list."
                };
            }
            catch (Exception)
            {
                return new BaseApprovedEmailResult
                {
                    Success = false,
                    Message = "An error occurred while removing the approved email.",
                    Errors = ["Internal server error"]
                };
            }
        }
    }
}
