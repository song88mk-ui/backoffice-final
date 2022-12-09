using Backoffice.Shared.Enums;

namespace Backoffice.Shared.Models.Requests
{
    public class GetClientListRequest
    {
        public string Search { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public ClientSorting SortBy { get; set; }
    }
}
