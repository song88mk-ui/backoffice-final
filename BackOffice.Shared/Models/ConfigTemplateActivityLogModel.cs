using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ConfigTemplateActivityLogModel
    {
        public string TemplateName { get; set; }
        public List<ConfigTemplateActivityLogPayloadModel> Payloads { get; set; }
    }

    public class ConfigTemplateActivityLogPayloadModel
    {
        public List<string> Clients { get; set; }
        public List<string> Leagues { get; set; }
    }
}
