using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Dtos.Users
{
    public class AssignManagerRequest
    {
        [Required(ErrorMessage = "Manager ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Manager ID must be a positive number.")]
        public int ManagerId { get; set; }
    }
}