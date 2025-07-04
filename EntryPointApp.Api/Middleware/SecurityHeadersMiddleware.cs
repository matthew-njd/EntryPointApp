namespace EntryPointApp.Api.Middleware
{
    public class SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            AddSecurityHeaders(context);

            await _next(context);
        }

        private static void AddSecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            headers.Append("X-Frame-Options", "DENY");

            headers.Append("X-Content-Type-Options", "nosniff");

            headers.Append("X-XSS-Protection", "1; mode=block");

            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none';");

            headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
        }
    }
}