using Backoffice.Shared.Models.Response;
using System;

namespace Backoffice.Shared.Models.Requests
{
    public class ClientDeleteMarketTemplateGroupRequest
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public ClientMarketTemplateGroupDTO MarketTemplateGroup { get; set; }
    }
}
