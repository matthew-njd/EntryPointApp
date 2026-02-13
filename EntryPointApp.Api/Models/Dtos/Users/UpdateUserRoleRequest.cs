using System.ComponentModel.DataAnnotations;
using EntryPointApp.Api.Models.Enums;

namespace EntryPointApp.Api.Models.Dtos.Users
{
    public class UpdateUserRoleRequest
    {
        [Required(ErrorMessage = "Role is required.")]
        public UserRole Role { get; set; }   
    }
}