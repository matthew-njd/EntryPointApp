namespace EntryPointApp.Api.Models.Dtos.Users
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Role { get; set; } = string.Empty;
        public int? ManagerId { get; set; }
    }
}