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
    }
}