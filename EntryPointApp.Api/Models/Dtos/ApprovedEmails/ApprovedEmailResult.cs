namespace EntryPointApp.Api.Models.Dtos.ApprovedEmails
{
    public class BaseApprovedEmailResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
    }

    public class ApprovedEmailResult : BaseApprovedEmailResult
    {
        public ApprovedEmailResponse? Data { get; set; }
    }

    public class ApprovedEmailListResult : BaseApprovedEmailResult
    {
        public List<ApprovedEmailResponse>? Data { get; set; }
    }
}
