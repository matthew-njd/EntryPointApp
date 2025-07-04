using System.Net;
using System.Text.Json;
using EntryPointApp.Api.Exceptions;
using EntryPointApp.Api.Models.Common;

namespace EntryPointApp.Api.Middleware
{
    public class GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;
        private readonly IWebHostEnvironment _environment = environment;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                Instance = context.Request.Path,
                TraceId = context.TraceIdentifier
            };

            switch (exception)
            {
                case ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Type = "ValidationError";
                    errorResponse.Title = "Validation Failed";
                    errorResponse.Status = (int)HttpStatusCode.BadRequest;
                    errorResponse.Detail = validationEx.Message;
                    errorResponse.Extensions["errors"] = validationEx.Errors;
                    break;

                case UnauthorizedException unauthorizedEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Type = "AuthenticationError";
                    errorResponse.Title = "Authentication Failed";
                    errorResponse.Status = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Detail = unauthorizedEx.Message;
                    break;

                case ForbiddenException forbiddenEx:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse.Type = "AuthorizationError";
                    errorResponse.Title = "Access Denied";
                    errorResponse.Status = (int)HttpStatusCode.Forbidden;
                    errorResponse.Detail = forbiddenEx.Message;
                    break;

                case NotFoundException notFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Type = "ResourceNotFound";
                    errorResponse.Title = "Resource Not Found";
                    errorResponse.Status = (int)HttpStatusCode.NotFound;
                    errorResponse.Detail = notFoundEx.Message;
                    break;

                case BusinessException businessEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Type = "BusinessRuleViolation";
                    errorResponse.Title = "Business Rule Violation";
                    errorResponse.Status = (int)HttpStatusCode.BadRequest;
                    errorResponse.Detail = businessEx.Message;
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Type = "InternalServerError";
                    errorResponse.Title = "Internal Server Error";
                    errorResponse.Status = (int)HttpStatusCode.InternalServerError;
                    
                    if (_environment.IsDevelopment())
                    {
                        errorResponse.Detail = exception.Message;
                        errorResponse.Extensions["stackTrace"] = exception.StackTrace ?? "";
                    }
                    else
                    {
                        errorResponse.Detail = "An error occurred while processing your request.";
                    }
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            });

            await response.WriteAsync(jsonResponse);
        }
    }
}