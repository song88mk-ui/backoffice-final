using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class ClientSportOrderFilterModel
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public bool IsUpdate { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
    }
}
