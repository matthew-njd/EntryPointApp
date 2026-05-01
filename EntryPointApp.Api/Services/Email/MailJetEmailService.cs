using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;

namespace EntryPointApp.Api.Services.Email
{
    public class MailJetEmailService : IEmailService
    {
        private readonly IMailjetClient _mailjetClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailJetEmailService> _logger;

        public MailJetEmailService(
            IMailjetClient mailjetClient,
            IConfiguration configuration,
            ILogger<MailJetEmailService> logger)
        {
            _mailjetClient = mailjetClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            try
            {
                var fromEmail = _configuration["MailJet:FromEmail"]
                    ?? throw new InvalidOperationException("MailJet:FromEmail not configured");
                var fromName = _configuration["MailJet:FromName"] ?? "EntryPoint App";

                var email = new TransactionalEmailBuilder()
                            .WithFrom(new SendContact(fromEmail, fromName))
                            .WithTo(new SendContact(toEmail))
                            .WithSubject("Reset your password")
                            .WithHtmlPart($@"
                                <html>
                                <body style='margin:0; padding:0; background-color:#f4f6f8; font-family: Arial, Helvetica, sans-serif;'>
                                    <div style='max-width:600px; margin:40px auto; background-color:#ffffff; padding:32px; border-radius:6px; text-align:center;'>

                                        <!-- Title -->
                                        <h1 style='margin:0 0 16px 0; font-size:26px; color:#1f2a44; font-weight:bold;'>
                                            Reset your password
                                        </h1>

                                        <!-- Body text -->
                                        <p style='margin:0 0 30px 0; font-size:15px; color:#5f6b7a; line-height:1.6;'>
                                            You have requested to reset your password for your EntryPoint App account.<br>
                                            If you did not make this request, please ignore this email.
                                        </p>

                                        <!-- Button -->
                                        <a href='{resetLink}'
                                        style='display:inline-block;
                                                background-color:#1c4e80;
                                                color:#ffffff;
                                                text-decoration:none;
                                                padding:14px 36px;
                                                font-size:15px;
                                                font-weight:bold;
                                                border-radius:4px;'>
                                            Reset your password
                                        </a><br>

                                        <!-- Spacer -->
                                        <div style='height:40px;'></div>

                                        <!-- Expiry notice -->
                                        <p style='font-size:12px; color:#9aa3ad; line-height:1.5; margin:0 0 10px 0;'>
                                            This password reset link will expire in 1 hour.<br>
                                            If you didn't request this password reset, please ignore this email. Your password will remain unchanged.
                                        </p>

                                        <!-- Fallback link -->
                                        <p style='font-size:12px; color:#9aa3ad; line-height:1.5;'>
                                            If the button doesn't work, copy and paste this link into your browser:
                                            <br />
                                            <a href='{resetLink}' style='color:#1c4e80; word-break:break-all;'>
                                                {resetLink}
                                            </a>
                                        </p>

                                        <hr style='border:none; border-top:1px solid #e6e9ec; margin:30px 0;' />

                                        <!-- Footer -->
                                        <p style='font-size:11px; color:#b0b7c3; margin:0;'>
                                            © {DateTime.Now.Year} The Postcard Factory. All rights reserved.
                                        </p>

                                    </div>
                                </body>
                                </html>
                            ")
                            .WithTextPart($@"
                                Password Reset Request

                                You have requested to reset your password for your EntryPoint App account.

                                Click the link below to reset your password:
                                {resetLink}

                                This link will expire in 1 hour for security reasons.

                                If you didn't request this password reset, please ignore this email.
                                Your password will remain unchanged.
                            ")
                            .Build();

                var response = await _mailjetClient.SendTransactionalEmailAsync(email);

                if (response.Messages != null && response.Messages.Length > 0)
                {
                    var message = response.Messages[0];
                    if (message.Status == "success")
                    {
                        _logger.LogInformation("Password reset email sent successfully to {Email}", toEmail);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send password reset email to {Email}. Status: {Status}",
                            toEmail, message.Status);
                        return false;
                    }
                }

                _logger.LogWarning("No response messages from MailJet for {Email}", toEmail);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendTimesheetApprovalEmailAsync(
            string employeeEmail,
            string employeeName,
            string managerEmail,
            string managerName,
            string weekPeriod,
            decimal totalHours,
            decimal totalCharges,
            byte[] excelAttachment,
            string filename)
        {
            try
            {
                var fromEmail = _configuration["MailJet:FromEmail"]
                    ?? throw new InvalidOperationException("MailJet:FromEmail not configured");
                var fromName = _configuration["MailJet:FromName"] ?? "EntryPoint Timesheets";
                var hrEmail = _configuration["MailJet:HrEmail"]
                    ?? throw new InvalidOperationException("MailJet:HrEmail not configured");

                _logger.LogInformation("Sending timesheet approval email to Employee: {EmployeeEmail}, Manager: {ManagerEmail}, HR: {HrEmail}",
                    employeeEmail, managerEmail, hrEmail);

                var htmlBody = $@"
                    <html>
                    <body style='margin:0; padding:0; background-color:#f4f6f8; font-family: Arial, Helvetica, sans-serif;'>
                        <div style='max-width:600px; margin:40px auto; background-color:#ffffff; padding:32px; border-radius:6px;'>

                            <!-- Title -->
                            <h1 style='margin:0 0 16px 0; font-size:26px; color:#1f2a44; font-weight:bold; text-align:center;'>
                                ✓ Timesheet Approved
                            </h1>

                            <!-- Body text -->
                            <p style='margin:0 0 20px 0; font-size:15px; color:#5f6b7a; line-height:1.6;'>
                                <strong>{employeeName}'s</strong> timesheet for <strong>{weekPeriod}</strong> has been approved by <strong>{managerName}</strong>.
                            </p>

                            <!-- Summary Box -->
                            <div style='background-color:#f8f9fa; border-left:4px solid #22c55e; padding:16px; margin:20px 0;'>
                                <h3 style='margin:0 0 12px 0; font-size:16px; color:#1f2a44;'>Summary</h3>
                                <table style='width:100%; font-size:14px; color:#5f6b7a;'>
                                    <tr>
                                        <td style='padding:4px 0;'><strong>Employee:</strong></td>
                                        <td style='padding:4px 0; text-align:right;'>{employeeName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:4px 0;'><strong>Week Period:</strong></td>
                                        <td style='padding:4px 0; text-align:right;'>{weekPeriod}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:4px 0;'><strong>Total Hours:</strong></td>
                                        <td style='padding:4px 0; text-align:right;'>{totalHours:F2} hrs</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:4px 0;'><strong>Total Charges:</strong></td>
                                        <td style='padding:4px 0; text-align:right;'>${totalCharges:F2}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:4px 0;'><strong>Approved By:</strong></td>
                                        <td style='padding:4px 0; text-align:right;'>{managerName}</td>
                                    </tr>
                                </table>
                            </div>

                            <p style='margin:20px 0 0 0; font-size:15px; color:#5f6b7a; line-height:1.6;'>
                                Please see the attached Excel file for complete timesheet details including daily breakdown.
                            </p>

                            <hr style='border:none; border-top:1px solid #e6e9ec; margin:30px 0;' />

                            <!-- Footer -->
                            <p style='font-size:11px; color:#b0b7c3; margin:0; text-align:center;'>
                                © {DateTime.Now.Year} The Postcard Factory. All rights reserved.<br>
                                This is an automated notification from the EntryPoint Timesheet System.
                            </p>

                        </div>
                    </body>
                    </html>
                ";

                var textBody = $@"
                    TIMESHEET APPROVED

                    {employeeName}'s timesheet for {weekPeriod} has been approved by {managerName}.

                    Summary:
                    Employee: {employeeName}
                    Week Period: {weekPeriod}
                    Total Hours: {totalHours:F2} hrs
                    Total Charges: ${totalCharges:F2}
                    Approved By: {managerName}

                    Please see the attached Excel file for complete timesheet details.

                    ---
                    This is an automated notification from the EntryPoint Timesheet System.
                ";

                var attachment = new Attachment(
                    filename,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    Convert.ToBase64String(excelAttachment)
                );

                // Create email with attachment
                var email = new TransactionalEmailBuilder()
                    .WithFrom(new SendContact(fromEmail, fromName))
                    .WithTo(new SendContact(hrEmail, "HR Department"))
                    .WithCc(new SendContact(employeeEmail, employeeName))
                    .WithCc(new SendContact(managerEmail, managerName))
                    .WithSubject($"Timesheet Approved - {employeeName} - {weekPeriod}")
                    .WithHtmlPart(htmlBody)
                    .WithTextPart(textBody)
                    .WithAttachment(attachment)
                    .Build();

                var response = await _mailjetClient.SendTransactionalEmailAsync(email);

                if (response.Messages != null && response.Messages.Length > 0)
                {
                    var message = response.Messages[0];
                    if (message.Status == "success")
                    {
                        _logger.LogInformation("Timesheet approval email sent successfully for {EmployeeName}", employeeName);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send timesheet approval email for {EmployeeName}. Status: {Status}",
                            employeeName, message.Status);
                        return false;
                    }
                }

                _logger.LogWarning("No response messages from MailJet for timesheet approval email");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending timesheet approval email for {EmployeeName}", employeeName);
                return false;
            }
        }

        public async Task<bool> SendTimesheetSubmissionEmailAsync(
            string managerEmail,
            string managerName,
            string employeeName,
            string weekPeriod,
            decimal totalHours,
            decimal totalCharges,
            string timesheetUrl)
        {
            try
            {
                var fromEmail = _configuration["MailJet:FromEmail"]
                    ?? throw new InvalidOperationException("MailJet:FromEmail not configured");
                var fromName = _configuration["MailJet:FromName"] ?? "EntryPoint App";

                _logger.LogInformation("Sending timesheet submission email to Manager: {ManagerEmail} for Employee: {EmployeeName}",
                    managerEmail, employeeName);

                var htmlBody = $@"
                    <html>
                    <body style='margin:0; padding:0; background-color:#f4f6f8; font-family: Arial, Helvetica, sans-serif;'>
                        <div style='max-width:600px; margin:40px auto; background-color:#ffffff; padding:32px; border-radius:6px;'>

                            <!-- Title -->
                            <h1 style='margin:0 0 16px 0; font-size:26px; color:#1f2a44; font-weight:bold; text-align:center;'>
                                Timesheet Submitted for Approval
                            </h1>

                            <!-- Body text -->
                            <p style='margin:0 0 20px 0; font-size:15px; color:#5f6b7a; line-height:1.6;'>
                                Hi <strong>{managerName}</strong>,<br><br>
                                <strong>{employeeName}</strong> has submitted their timesheet for <strong>{weekPeriod}</strong> and is awaiting your approval.
                            </p>

                            <!-- Summary Box -->
                            <div style='background-color:#f8f9fa; border-left:4px solid #1c4e80; padding:16px; margin:20px 0;'>
                                <h3 style='margin:0 0 12px 0; font-size:16px; color:#1f2a44;'>Summary</h3>
                                <table style='width:100%; font-size:14px; color:#5f6b7a;'>
                                    <tr>
                                        <td style='padding:4px 0;'><strong>Employee:</strong></td>
                                        <td style='padding:4px 0; text-align:right;'>{employeeName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:4px 0;'><strong>Week Period:</strong></td>
                                        <td style='padding:4px 0; text-align:right;'>{weekPeriod}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:4px 0;'><strong>Total Hours:</strong></td>
                                        <td style='padding:4px 0; text-align:right;'>{totalHours:F2} hrs</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:4px 0;'><strong>Total Charges:</strong></td>
                                        <td style='padding:4px 0; text-align:right;'>${totalCharges:F2}</td>
                                    </tr>
                                </table>
                            </div>

                            <!-- Review Button -->
                            <div style='text-align:center; margin:30px 0;'>
                                <a href='{timesheetUrl}'
                                style='display:inline-block;
                                        background-color:#1c4e80;
                                        color:#ffffff;
                                        text-decoration:none;
                                        padding:14px 36px;
                                        font-size:15px;
                                        font-weight:bold;
                                        border-radius:4px;'>
                                    Review Timesheet
                                </a>
                            </div>

                            <!-- Fallback link -->
                            <p style='font-size:12px; color:#9aa3ad; line-height:1.5; text-align:center;'>
                                If the button doesn't work, copy and paste this link into your browser:
                                <br />
                                <a href='{timesheetUrl}' style='color:#1c4e80; word-break:break-all;'>
                                    {timesheetUrl}
                                </a>
                            </p>

                            <hr style='border:none; border-top:1px solid #e6e9ec; margin:30px 0;' />

                            <!-- Footer -->
                            <p style='font-size:11px; color:#b0b7c3; margin:0; text-align:center;'>
                                © {DateTime.Now.Year} The Postcard Factory. All rights reserved.<br>
                                This is an automated notification from the EntryPoint Timesheet System.
                            </p>

                        </div>
                    </body>
                    </html>
                ";

                var textBody = $@"
                    TIMESHEET SUBMITTED FOR APPROVAL

                    Hi {managerName},

                    {employeeName} has submitted their timesheet for {weekPeriod} and is awaiting your approval.

                    Summary:
                    Employee: {employeeName}
                    Week Period: {weekPeriod}
                    Total Hours: {totalHours:F2} hrs
                    Total Charges: ${totalCharges:F2}

                    Review the timesheet here:
                    {timesheetUrl}

                    ---
                    This is an automated notification from the EntryPoint Timesheet System.
                ";

                var email = new TransactionalEmailBuilder()
                    .WithFrom(new SendContact(fromEmail, fromName))
                    .WithTo(new SendContact(managerEmail, managerName))
                    .WithSubject($"Timesheet Submitted for Approval - {employeeName} - {weekPeriod}")
                    .WithHtmlPart(htmlBody)
                    .WithTextPart(textBody)
                    .Build();

                var response = await _mailjetClient.SendTransactionalEmailAsync(email);

                if (response.Messages != null && response.Messages.Length > 0)
                {
                    var message = response.Messages[0];
                    if (message.Status == "success")
                    {
                        _logger.LogInformation("Timesheet submission email sent successfully to {ManagerEmail} for {EmployeeName}",
                            managerEmail, employeeName);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send timesheet submission email to {ManagerEmail}. Status: {Status}",
                            managerEmail, message.Status);
                        return false;
                    }
                }

                _logger.LogWarning("No response messages from MailJet for timesheet submission email");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending timesheet submission email for {EmployeeName}", employeeName);
                return false;
            }
        }
    }
}