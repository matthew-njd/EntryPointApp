using System.ComponentModel.DataAnnotations;

namespace EntryPointApp.Api.Models.Dtos.Users
{
    public class AssignSalesRepRequest
    {
        [Required(ErrorMessage = "Sales Rep ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Sales Rep ID must be a positive number.")]
        public int SalesRepId { get; set; }
    }
}
