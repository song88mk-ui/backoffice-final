using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ConfigTemplateLogModel
    {
        public string TemplateName { get; set; }
        public ConfigTemplateLogPayloadModel Payloads { get; set; }
    }

    public class ConfigTemplateLogPayloadModel
    {
        public List<string> Clients { get; set; }
        public List<string> Leagues { get; set; }
    }
}
