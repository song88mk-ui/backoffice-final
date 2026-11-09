using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ClientCopyMarketTemplateGroupRequest
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        // Single target, as sent before BO-158 and to the Trading API (which takes one target per call).
        public Guid CopyToClientId { get; set; }
        // BO-158: every target client. When it is empty the API falls back to CopyToClientId.
        public List<Guid> CopyToClientIds { get; set; } = new List<Guid>();
        public int CopyToSportId { get; set; }
        public int CopyToFixtureStatusId { get; set; }
    }
}
