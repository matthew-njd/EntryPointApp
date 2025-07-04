using EntryPointApp.Api.Configuration;
using EntryPointApp.Api.Middleware;

namespace EntryPointApp.Api.Extensions
{
    public static class SecurityExtensions
    {
        public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
        {
            var corsSettings = configuration.GetSection("CorsSettings").Get<CorsSettings>();
            
            if (corsSettings != null)
            {
                services.AddCors(options =>
                {
                    options.AddPolicy("DefaultCorsPolicy", builder =>
                    {
                        if (corsSettings.AllowedOrigins.Any())
                        {
                            builder.WithOrigins(corsSettings.AllowedOrigins);
                        }
                        else
                        {
                            builder.WithOrigins("http://localhost:3000", "https://localhost:3000", 
                                              "http://localhost:4200", "https://localhost:4200");
                        }

                        if (corsSettings.AllowedMethods.Any())
                        {
                            builder.WithMethods(corsSettings.AllowedMethods);
                        }
                        else
                        {
                            builder.WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS");
                        }

                        if (corsSettings.AllowedHeaders.Any())
                        {
                            builder.WithHeaders(corsSettings.AllowedHeaders);
                        }
                        else
                        {
                            builder.WithHeaders("Content-Type", "Authorization", "X-Requested-With");
                        }

                        if (corsSettings.AllowCredentials)
                        {
                            builder.AllowCredentials();
                        }

                        builder.SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.PreflightMaxAge));
                    });
                });
            }
            else
            {
                // Default CORS for development
                services.AddCors(options =>
                {
                    options.AddPolicy("DefaultCorsPolicy", builder =>
                    {
                        builder.WithOrigins("http://localhost:3000", "https://localhost:3000", 
                                          "http://localhost:4200", "https://localhost:4200")
                               .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                               .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                               .SetPreflightMaxAge(TimeSpan.FromHours(24));
                    });
                });
            }

            return services;
        }

        public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder app)
        {
            // Order matters - security headers should be added early
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();
            
            return app;
        }
    }
}