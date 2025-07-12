using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }

        [MaxLength(500)]
        public required string Token { get; set; } = string.Empty;

        public required int UserId { get; set; }

        public required DateTime ExpiryDate { get; set; }

        public required DateTime CreatedAt { get; set; }
        
        public required bool IsRevoked { get; set; } = false;

        [MaxLength(500)]
        public string? ReplacedBy { get; set; }
        
        public DateTime? RevokedAt { get; set; }
        
        public User? User { get; set; }
    }
}