using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class CopyClientConfigResponse
    {
        // True when every target was copied.
        public bool Result { get; set; }

        // The assignments removed from the copied targets for leagues the source client has no assignment for.
        public List<ConfigTemplateAssigned> ToBeRemoved { get; set; } = new List<ConfigTemplateAssigned>();

        // BO-162: one entry per target client.
        public List<CopyClientConfigTargetResult> Targets { get; set; } = new List<CopyClientConfigTargetResult>();
    }

    public class CopyClientConfigTargetResult
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
    }
}
