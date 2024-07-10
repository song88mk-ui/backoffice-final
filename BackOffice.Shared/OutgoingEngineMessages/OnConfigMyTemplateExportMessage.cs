using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnConfigMyTemplateExportMessage
    {
        public string FileName { get; set; }
        public string UserId { get; set; }
        public string ConnectionId { get; set; } = "";
        public Dictionary<int, List<MyTemplateExportPayloadModel>> SportPayloads { get; set; }
    }
    public class MyTemplateExportPayloadModel
    {
        public string ClientCode { get; set; }
        public List<int> LeagueIds { get; set; }
    }
}
