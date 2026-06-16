using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Entities
{
    public class ApprovedEmail
    {
        public int Id { get; set; }
        [StringLength(100)]
        public required string Email { get; set; }
        public int? AddedByAdminId { get; set; }
        public DateTime CreatedAt { get; set; }

        public User? AddedByAdmin { get; set; }
    }
}
