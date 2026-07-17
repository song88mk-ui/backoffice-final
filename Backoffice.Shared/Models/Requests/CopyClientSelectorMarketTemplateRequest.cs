using System;

namespace Backoffice.Shared.Models.Requests
{
    public class CopyClientSelectorMarketTemplateRequest
    {
        public Guid CopyFromClientId { get; set; }
        public Guid CopyToClientId { get; set; }
        public string CopyToClientCode { get; set; }
        public string UserId { get; set; }
    }
}
