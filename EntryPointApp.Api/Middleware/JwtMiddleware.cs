using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EntryPointApp.Api.Services.Authentication;

namespace EntryPointApp.Api.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IJwtService jwtService, IAuthenticationService authService)
        {
            var token = context.Request.Headers.Authorization
                .FirstOrDefault()?.Split(" ").Last();

            if (token != null)
            {
                await AttachUserToContext(context, jwtService, authService, token);
            }

            await _next(context);
        }

        private async Task AttachUserToContext(HttpContext context, IJwtService jwtService, IAuthenticationService authService, string token)
        {
            try
            {
                if (jwtService.ValidateToken(token))
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                    if (userId != null && int.TryParse(userId, out var id))
                    {
                        var user = await authService.GetUserByIdAsync(id);
                        if (user != null && user.IsActive)
                        {
                            context.Items["User"] = user;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error attaching user to context");
            }
        }
    }
}