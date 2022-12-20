using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ClientSportOrderListResponse
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
