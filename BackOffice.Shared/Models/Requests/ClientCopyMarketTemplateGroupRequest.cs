using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ClientCopyMarketTemplateGroupRequest
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public Guid CopyToClientId { get; set; }
        public int CopyToSportId { get; set; }
        public int CopyToFixtureStatusId { get; set; }
    }
}
