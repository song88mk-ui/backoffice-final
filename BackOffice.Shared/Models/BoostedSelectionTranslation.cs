using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class BoostedSelectionTranslation
    {
        public Language Language { get; set; }
        public string ClientCode { get; set; }
        public string BoostedSelectionName { get; set; }
        public bool SelectLanguage { get; set; }
        public bool IsRequired { get; set; }
    }
}
