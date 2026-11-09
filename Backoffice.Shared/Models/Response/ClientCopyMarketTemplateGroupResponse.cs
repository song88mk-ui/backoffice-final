using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    // BO-158: the outcome of copying one client's market groups to several clients, one entry per target.
    public class ClientCopyMarketTemplateGroupResponse
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public List<ClientCopyMarketTemplateGroupTargetResult> Targets { get; set; } = new List<ClientCopyMarketTemplateGroupTargetResult>();
    }

    public class ClientCopyMarketTemplateGroupTargetResult
    {
        public Guid ClientId { get; set; }
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
        // The target's groups before the copy replaced them, for the activity log.
        public List<ClientMarketTemplateGroupDTO> PreviousGroups { get; set; } = new List<ClientMarketTemplateGroupDTO>();
        // The target's groups after the copy, as the Trading API returned them.
        public List<ClientMarketTemplateGroupDTO> CopiedGroups { get; set; } = new List<ClientMarketTemplateGroupDTO>();
    }
}
