using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class NoMarginClientsResponse
    {
        public List<string> ClientCodes { get; set; } = new List<string>();
        public List<string> MarketTemplateIds { get; set; } = new List<string>();
    }
}
