namespace EntryPointApp.Api.Models.Dtos.ApprovedEmails
{
    public class ApprovedEmailResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public int? AddedByAdminId { get; set; }
        public string? AddedByAdminName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
