using Backoffice.Shared.Data.Backoffice.Entities;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class CopyClientConfigResponse
    {
        public bool Result { get; set; }
        public List<ConfigTemplateAssigned> ToBeRemoved { get; set; }
    }
}
