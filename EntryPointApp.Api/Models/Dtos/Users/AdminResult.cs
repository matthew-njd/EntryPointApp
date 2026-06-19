using EntryPointApp.Api.Models.Dtos.Common;

namespace EntryPointApp.Api.Models.Dtos.Users
{
    public class BaseAdminResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
    }

    public class UserResult : BaseAdminResult
    {
        public UserDto? Data { get; set; }
    }

    public class UserSummaryDto
    {
        public int TotalUsers { get; set; }
        public int TotalSalesReps { get; set; }
        public int TotalManagers { get; set; }
        public int TotalAdmins { get; set; }
        public int ActiveUsers { get; set; }
    }

    public class UserListResponse
    {
        public List<UserDto> Users { get; set; } = [];
        public UserSummaryDto Summary { get; set; } = new();
    }

    public class UserListResult : BaseAdminResult
    {
        public UserListResponse? Data { get; set; }
    }

    public class UserPagedResponse : PagedResult<UserDto>
    {
        public UserSummaryDto Summary { get; set; } = new();
    }

    public class UserPagedResult : BaseAdminResult
    {
        public UserPagedResponse? Data { get; set; }
    }

    public class UserRateResult : BaseAdminResult
    {
        public UserRateDto? Data { get; set; }
    }

    public class UserRateListResult : BaseAdminResult
    {
        public List<UserRateDto>? Data { get; set; }
    }
}