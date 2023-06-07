using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class TemplateConfigCount
    {
        public int MyTemplatesCount { get; set; }
        public int AssignedTemplatesCount { get; set; }
        public int UnAssignedTemplatesCount { get; set; }
    }
}
