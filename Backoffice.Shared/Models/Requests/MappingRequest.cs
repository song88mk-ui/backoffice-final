namespace Backoffice.Shared.Models.Requests
{
    public class MappingRequest
    {
        public string SearchText { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; } = 20;
    }
}
