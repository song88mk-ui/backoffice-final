using Backoffice.Shared.Enums;

namespace Backoffice.Shared.Models.Requests
{
    public class GetClientListRequest
    {
        public string Search { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public ClientSorting SortBy { get; set; }

        // Fetches the full Platform client list to fill LimitId/Endpoint — only callers that edit
        // clients need it, so it's opt-in to keep paged callers (e.g. Dashboards) cheap.
        public bool IncludePlatformFields { get; set; }
    }
}
