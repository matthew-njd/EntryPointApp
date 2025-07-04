namespace EntryPointApp.Api.Models.Dtos.Authentication
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public LoginResponse? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}