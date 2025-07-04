using EntryPointApp.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;

namespace EntryPointApp.Api.Services.Authentication
{
    public class AuthorizationPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string ManagerOrAdmin = "ManagerOrAdmin";
        public const string UserOrAbove = "UserOrAbove";

        public static void ConfigurePolicies(AuthorizationOptions options)
        {
            options.AddPolicy(AdminOnly, policy => 
                policy.RequireRole(UserRole.Admin.ToString()));

            options.AddPolicy(ManagerOrAdmin, policy => 
                policy.RequireRole(UserRole.Manager.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy(UserOrAbove, policy => 
                policy.RequireRole(UserRole.User.ToString(), UserRole.Manager.ToString(), UserRole.Admin.ToString()));
        }
    }
}