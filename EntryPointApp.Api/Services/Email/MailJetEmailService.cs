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
                    .WithSubject("Password Reset Request")
                    .WithHtmlPart($@"
                        <html>
                        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                                <h2 style='color: #2c3e50;'>Password Reset Request</h2>
                                <p>You have requested to reset your password for your EntryPoint App account.</p>
                                <p>Click the button below to reset your password:</p>
                                <div style='text-align: center; margin: 30px 0;'>
                                    <a href='{resetLink}' 
                                       style='background-color: #3498db; 
                                              color: white; 
                                              padding: 12px 30px; 
                                              text-decoration: none; 
                                              border-radius: 5px;
                                              display: inline-block;'>
                                        Reset Password
                                    </a>
                                </div>
                                <p style='color: #7f8c8d; font-size: 14px;'>
                                    This link will expire in 1 hour for security reasons.
                                </p>
                                <p style='color: #7f8c8d; font-size: 14px;'>
                                    If you didn't request this password reset, please ignore this email. 
                                    Your password will remain unchanged.
                                </p>
                                <hr style='border: none; border-top: 1px solid #ecf0f1; margin: 20px 0;'>
                                <p style='color: #95a5a6; font-size: 12px;'>
                                    If the button doesn't work, copy and paste this link into your browser:<br>
                                    <a href='{resetLink}' style='color: #3498db;'>{resetLink}</a>
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