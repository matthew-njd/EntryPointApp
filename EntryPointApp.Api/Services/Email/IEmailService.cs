namespace EntryPointApp.Api.Services.Email
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);
    }
}