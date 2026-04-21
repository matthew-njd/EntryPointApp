using EntryPointApp.Api.Models.Dtos.Users;

namespace EntryPointApp.Api.Services.Admin
{
    public interface IUserRateService
    {
        Task<UserRateListResult> GetRatesForUserAsync(int userId);
        Task<UserRateResult> GetCurrentRateAsync(int userId);
        Task<UserRateResult> SetRateAsync(int userId, SetUserRateRequest request, int adminId);
    }
}
