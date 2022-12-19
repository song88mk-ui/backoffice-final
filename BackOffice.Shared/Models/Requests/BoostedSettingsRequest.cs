using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class BoostedSettingsRequest
    {
        public List<string> ClientCodes { get; set; }
        public string Search { get; set; } = string.Empty;
    }
}
