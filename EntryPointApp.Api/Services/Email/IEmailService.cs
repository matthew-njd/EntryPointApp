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
            string? salesRepEmail,
            string? salesRepName,
            string weekPeriod,
            decimal totalHours,
            decimal totalCharges,
            decimal hourlyRate,
            decimal totalPay,
            decimal mileageRate,
            decimal mileagePay,
            byte[] excelAttachment,
            string filename);

        Task<bool> SendTimesheetSubmissionEmailAsync(
            string managerEmail,
            string managerName,
            string employeeName,
            string weekPeriod,
            decimal totalHours,
            decimal totalCharges,
            string timesheetUrl);

        Task<bool> SendTimesheetDenialEmailAsync(
            string employeeEmail,
            string employeeName,
            string deniedByName,
            string weekPeriod,
            decimal totalHours,
            decimal totalCharges,
            string denialReason,
            string timesheetUrl);

        Task<bool> SendSalesRepSubmissionEmailAsync(
            string salesRepEmail,
            string salesRepName,
            string employeeName,
            string weekPeriod,
            decimal totalHours,
            decimal totalCharges,
            string timesheetUrl);

        Task<bool> SendManagerNotificationEmailAsync(
            string managerEmail,
            string managerName,
            string salesRepName,
            string employeeName,
            string weekPeriod,
            decimal totalHours,
            decimal totalCharges,
            string timesheetUrl);
    }
}