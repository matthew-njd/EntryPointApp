using EntryPointApp.Api.Models.Dtos.ApprovedEmails;

namespace EntryPointApp.Api.Services.ApprovedEmails
{
    public interface IApprovedEmailService
    {
        Task<ApprovedEmailListResult> GetAllAsync();
        Task<ApprovedEmailResult> AddAsync(AddApprovedEmailRequest request, int adminId);
        Task<BaseApprovedEmailResult> RemoveAsync(int id);
    }
}
