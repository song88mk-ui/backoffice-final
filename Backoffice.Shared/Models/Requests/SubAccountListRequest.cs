
namespace Backoffice.Shared.Models.Requests
{
    public class SubAccountListRequest
    {
        public string MainUserId { get; set; }
        public string SearchText { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
