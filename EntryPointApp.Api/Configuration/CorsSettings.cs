namespace EntryPointApp.Api.Configuration
{
    public class CorsSettings
    {
        public string[] AllowedOrigins { get; set; } = [];
        public string[] AllowedMethods { get; set; } = [];
        public string[] AllowedHeaders { get; set; } = [];
        public bool AllowCredentials { get; set; } = false;
        public int PreflightMaxAge { get; set; } = 86400;
    }
}