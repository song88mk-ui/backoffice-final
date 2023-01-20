namespace Backoffice.Web.Models
{
    public class ClientSegmentSortOrderModel
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int SortOrder { get; set; }
    }
}
