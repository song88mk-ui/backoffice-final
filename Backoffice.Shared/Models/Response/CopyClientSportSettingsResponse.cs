using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    // BO-160: the outcome of copying Sport Settings to several clients, one entry per target.
    public class CopyClientSportSettingsResponse
    {
        public List<CopyClientSportSettingsTargetResult> Targets { get; set; } = new List<CopyClientSportSettingsTargetResult>();
    }

    public class CopyClientSportSettingsTargetResult
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
    }
}
