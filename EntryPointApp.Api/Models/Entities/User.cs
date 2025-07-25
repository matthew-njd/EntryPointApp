using System.ComponentModel.DataAnnotations;
using EntryPointApp.Api.Models.Enums;

namespace EntryPointApp.Api.Models.Entities
{
    public class User
    {
        public int Id { get; set; }

        [StringLength(100)]
        public required string Email { get; set; }

        public required string PasswordHash { get; set; }

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        public required UserRole Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? ManagerId { get; set; }

        public bool IsManager { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public User? Manager { get; set; }
        public ICollection<User> ManagedUsers { get; set; } = [];
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
        public ICollection<WeeklyLog> WeeklyLogs { get; set; } = [];
    }
}