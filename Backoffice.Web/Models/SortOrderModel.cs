using Newtonsoft.Json;

namespace Backoffice.Web.Models
{
    public class SortOrderModel
    {
        [JsonProperty(PropertyName = "rowId")]
        public string RowId { get; set; }
        [JsonProperty(PropertyName = "sortOrder")]
        public int SortOrder { get; set; }
    }
}
