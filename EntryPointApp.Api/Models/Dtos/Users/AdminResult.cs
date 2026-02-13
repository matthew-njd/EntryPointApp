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

    public class UserListResult : BaseAdminResult
    {
        public List<UserDto>? Data { get; set; }
    }
}