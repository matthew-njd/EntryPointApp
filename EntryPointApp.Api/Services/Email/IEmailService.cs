namespace EntryPointApp.Api.Services.Email
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task<bool> SendTimesheetApprovalEmailAsync(
            string employeeEmail, 
            string employeeName, 
            string managerEmail,
            string managerName,
            string weekPeriod, 
            decimal totalHours, 
            decimal totalCharges,
            byte[] excelAttachment,
            string filename);
    }
}