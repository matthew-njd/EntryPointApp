using EntryPointApp.Api.Models.Dtos.Authentication;
using EntryPointApp.Api.Services.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntryPointApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthenticationService authService, ILogger<AuthController> logger) : ControllerBase
    {
        private readonly IAuthenticationService _authService = authService;
        private readonly ILogger<AuthController> _logger = logger;

    }
}