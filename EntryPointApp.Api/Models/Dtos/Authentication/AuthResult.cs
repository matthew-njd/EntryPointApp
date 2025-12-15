namespace EntryPointApp.Api.Models.Dtos.Authentication
{
    public class BaseAuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
    }

    public class RegisterAuthResult : BaseAuthResult
    {
        public RegisterResponse? Data { get; set; }
    }

    public class LoginAuthResult : BaseAuthResult
    {
        public LoginResponse? Data { get; set; }
    }

    public class RefreshTokenAuthResult : BaseAuthResult
    {
        public LoginResponse? Data { get; set; }
    }

    public class AuthResult : BaseAuthResult
    {

    }

    public class ForgotPasswordAuthResult : BaseAuthResult
    {

    }

    public class ResetPasswordAuthResult : BaseAuthResult
    {

    }
}