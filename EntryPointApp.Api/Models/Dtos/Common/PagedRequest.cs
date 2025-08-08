namespace EntryPointApp.Api.Models.Dtos.Common
{
    public class PagedRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}