using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ConfigTemplateResponse
    {
        public List<ConfigTemplateModel> ConfigTemplates { get; set; }
        public MetaData MetaData { get; set; }
    }
}
